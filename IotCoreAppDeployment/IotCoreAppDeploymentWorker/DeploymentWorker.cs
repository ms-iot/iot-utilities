using IotCoreAppProjectExtensibility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fclp.Internals;
using System.ComponentModel;
using System.Reflection;

namespace IotCoreAppDeployment
{
    public class DeploymentWorker
    {
        #region Define arguments
        private Fclp.FluentCommandLineParser commandLineParser = null;
        public Fclp.FluentCommandLineParser CommandLineParser
        {
            get
            {
                if (commandLineParser == null)
                {
                    commandLineParser = new Fclp.FluentCommandLineParser();
                    commandLineParser.IsCaseSensitive = false;

                    commandLineParser.Setup<string>('s')
                        .WithDescription("Specify source input")
                        .Callback(value => { source = new FileInfo(value).FullName; })
                        .Required();

                    commandLineParser.Setup<string>('n')
                        .WithDescription("Speficy IoT Core device name or IP address")
                        .Callback(value => { targetName = value; })
                        .Required();

                    commandLineParser.Setup<string>('k')
                        .WithDescription(String.Format("Specify SDK version ... {0} is the default", defaultSdkVersion))
                        .Callback(value =>
                        {
                            sdk = GetSdkVersionFromString(value);
                            if (sdk == SdkVersion.Unknown)
                            {
                                StringBuilder sb = new StringBuilder();
                                var sdkVersionMembers = typeof(SdkVersion).GetEnumValues();
                                for (int i=0; i<sdkVersionMembers.Length; i++)
                                {
                                    var sdkVersionMember = (SdkVersion)sdkVersionMembers.GetValue(i);
                                    if (SdkVersion.Unknown == sdkVersionMember) continue;

                                    var field = typeof(SdkVersion).GetField(sdkVersionMember.ToString());
                                    var customAttributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                                    for (int j = 0; j < customAttributes.Length; j++)
                                    {
                                        var descriptionAttribute = customAttributes[j] as DescriptionAttribute;
                                        if (descriptionAttribute != null)
                                        {
                                            if (sb.Length != 0) sb.Append(", ");
                                            sb.Append(descriptionAttribute.Description);
                                        }
                                    }
                                }

                                OutputMessage(String.Format("Error: suported dependency sdks are: {0}", sb.ToString()));
                                throw new Fclp.OptionSyntaxException();
                            }
                        })
                        .SetDefault(defaultSdkVersion);

                    commandLineParser.Setup<DependencyConfiguration>('f')
                        .WithDescription("Specify the configuration [Debug|Release] ... Debug is the default")
                        .Callback(value => { configuration = value; })
                        .SetDefault(DependencyConfiguration.Debug);

                    commandLineParser.Setup<TargetPlatform>('a')
                        .WithDescription("Specify the target architecture [ARM|X86] ... ARM is the default")
                        .Callback(value => { targetType = value; })
                        .SetDefault(TargetPlatform.ARM);

                    commandLineParser.Setup<string>('u')
                        .WithDescription(String.Format("Specify target username ... {0} is the default", defaultTargetUserName))
                        .Callback(value => { credentials.UserName = value; })
                        .SetDefault(defaultTargetUserName);

                    commandLineParser.Setup<string>('p')
                        .WithDescription(String.Format("Specify target user password) ... {0} is the default", defaultTargetPassword))
                        .Callback(value => { credentials.Password = value; })
                        .SetDefault(defaultTargetPassword);

                    commandLineParser.Setup<string>('o')
                        .WithDescription("Specify full local path to output APPX to ... if this is not provided, files will not be saved")
                        .Callback(value => { copyOutputToFolder = value; });

                    commandLineParser.Setup<string>('x')
                        .WithDescription("Specify MakeAppx.exe full path ... if this is not provided, the registry is queried")
                        .Callback(value => { makeAppxPath = value; });

                    commandLineParser.Setup<string>('g')
                        .WithDescription("Specify SignTool.exe full path ... if this is not provided, the registry is queried")
                        .Callback(value => { signToolPath = value; });

                    commandLineParser.Setup<string>('w')
                        .WithDescription("Specify PowerShell.exe full path ... if this is not provided, the registry is queried")
                        .Callback(value => { powershellPath = value; });

                    commandLineParser.Setup<bool>('d')
                        .WithDescription("If this is specified, the temp folder will not be deleted (this is useful for diagnosing problems).")
                        .Callback(value => { keepTempFolder = value; })
                        .SetDefault(false);

                    commandLineParser.SetupHelp(new String[] { "?", "help", "h" })
                        .Callback(text =>
                        {
                            OutputMessage("");
                            OutputMessage(String.Format("  {0} -s (source) -n (target):", "IotCoreAppDeployment.exe"));
                            OutputMessage("");
                            foreach (var option in CommandLineParser.Options)
                            {
                                if (option.IsRequired) OutputMessage(String.Format("    -{0} (required)    {1}", option.ShortName, option.Description));
                            }
                            OutputMessage("");
                            var sortedOptions = new ICommandLineOption[CommandLineParser.Options.Count];
                            CommandLineParser.Options.CopyTo(sortedOptions);
                            Array.Sort(sortedOptions, (a, b) => { return a.ShortName.CompareTo(b.ShortName); });
                            foreach (var option in sortedOptions)
                            {
                                if (!option.IsRequired) OutputMessage(String.Format("    -{0}               {1}", option.ShortName, option.Description));
                            }
                            OutputMessage("");
                        });
                }
                return commandLineParser;
            }
        }

        #endregion

        private String outputFolder = "";
        private String source = "";
        private String targetName = "";
        private String makeAppxPath = null;
        private String signToolPath = null;
        private String powershellPath = null;
        private String copyOutputToFolder = null;
        private bool keepTempFolder = false;
        private TargetPlatform targetType = TargetPlatform.ARM;
        private SdkVersion sdk = SdkVersion.SDK_10_0_10586_0;
        private DependencyConfiguration configuration = DependencyConfiguration.Debug;

        private StreamWriter outputWriter;

        private const String packageFullNameFormat = "{0}_1.0.0.0_{1}__1w720vyc4ccym";

        private const String universalSdkRootKey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows Kits\Installed Roots";
        private const String universalSdkRootValue = @"KitsRoot10";

        private const String powershellRootKey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell";
        private const String powershellRootValue = @"Path";

        private const String defaultSdkVersion = "10.0.10586.0";
        private const String defaultTargetUserName = "Administrator";
        private const String defaultTargetPassword = "p@ssw0rd";
        private UserInfo credentials = new UserInfo() { UserName = defaultTargetUserName, Password = defaultTargetPassword };
        private const int QueryInterval = 3000;

        public static SdkVersion GetSdkVersionFromString(String sdk)
        {
            var sdkVersionMembers = typeof(SdkVersion).GetEnumValues();
            for (int i = 0; i < sdkVersionMembers.Length; i++)
            {
                var enumValue = (SdkVersion)sdkVersionMembers.GetValue(i);
                var field = typeof(SdkVersion).GetField(enumValue.ToString());
                var customAttributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                for (int j = 0; j < customAttributes.Length; j++)
                {
                    var descriptionAttribute = customAttributes[j] as DescriptionAttribute;
                    if (null != descriptionAttribute && descriptionAttribute.Description.Equals(sdk, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return enumValue;
                    }
                }
            }

            return SdkVersion.Unknown;
        }

        public void OutputMessage(String message)
        {
            outputWriter.WriteLine(message);
        }

        private void ExecuteExternalProcess(String executableFileName, String arguments, String logFileName)
        {
            Process process = new Process();
            process.StartInfo.FileName = executableFileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            var output = new StringBuilder();
            // Using WaitForExit would be cleaner, but for some reason, it
            // hangs when using MakeAppx.  In the process of debugging that,
            // I found that this never hangs.
            while (!process.HasExited)
            {
                output.Append(process.StandardOutput.ReadToEnd());
                Thread.Sleep(100);
            }

            using (var logStream = new StreamWriter(logFileName))
            {
                String errors = process.StandardError.ReadToEnd();
                if (errors != null && errors.Length != 0)
                {
                    logStream.WriteLine("\n\n\n\nErrors:");
                    logStream.Write(errors);
                }
                logStream.WriteLine("\n\n\n\nFull Output:");
                logStream.Write(output.ToString());
            }
        }

        private void NotifyThatMakeAppxOrSignToolNotFound()
        {
            OutputMessage("Error: MakeAppx.exe and SignTool.exe must be installed.  These tools ");
            OutputMessage("       are installed as part of the Windows Standalone SDK for Windows 10 ");
            OutputMessage("       (https://go.microsoft.com/fwlink/?LinkID=698771).  If they are ");
            OutputMessage("       present on your machine, please provide the paths using -makeappx ");
            OutputMessage("       and -signtool.");
        }

        private bool CopyBaseTemplateContents(ITemplate template)
        {
            var templateContents = template.GetTemplateContents();
            foreach (var content in templateContents)
            {
                var success = content.Apply(outputFolder);
                if (!success)
                {
                    Debug.WriteLine(String.Format("Failed to get {0} from resources.", content.AppxRelativePath));
                    return false;
                }
            }
            OutputMessage(String.Format("... base project files found and copied: {0}", outputFolder));
            return true;
        }

        private bool CopyProjectFiles(IProject project)
        {
            var appxContents = project.GetAppxContents();
            foreach (var content in appxContents)
            {
                var success = content.Apply(outputFolder);
                if (!success)
                {
                    Debug.WriteLine(String.Format("Failed to get {0} from resources.", content.AppxRelativePath));
                    return false;
                }
            }
            OutputMessage(String.Format("... project files found and copied: {0}", outputFolder));
            return true;
        }

        private bool SpecializeAppxManifest(IProject project)
        {
            var appxManifestChangess = project.GetAppxContentChanges();
            foreach (var change in appxManifestChangess)
            {
                var success = change.ApplyToContent(outputFolder);
                if (!success)
                {
                    Debug.WriteLine("Failed to make all changes to AppxManifest.xml.");
                    return false;
                }
            }
            OutputMessage("... project files tailored to current deployment.");
            return true;
        }

        private async Task<bool> BuildProjectAsync(IProject project)
        {
            OutputMessage("... build started");
            var buildSuccess = await project.BuildAsync(outputFolder, outputWriter);
            if (!buildSuccess)
            {
                OutputMessage("... build failed");
                return false;
            }
            OutputMessage("... build succeeded");
            return true;
        }

        private bool AddCapabilitiesToAppxManifest(IProject project)
        {
            var capabilityAdditions = project.GetCapabilities();
            foreach (var capability in capabilityAdditions)
            {
                var success = capability.ApplyToContent(outputFolder);
                if (!success)
                {
                    Debug.WriteLine("Failed to add all capabilities to AppxManifest.xml.");
                    return false;
                }
            }
            return true;
        }

        private bool CreateAppxMapFile(ITemplate template, IProject project, String mapFile)
        {
            var resourceMetadata = new List<String>();
            var appxFiles = new List<String>();
            bool success = template.GetAppxMapContents(resourceMetadata, appxFiles, outputFolder);
            if (!success)
            {
                Debug.WriteLine("Failed to get the template appx map contents.");
                return false;
            }
            success = project.GetAppxMapContents(resourceMetadata, appxFiles, outputFolder);
            if (!success)
            {
                Debug.WriteLine("Failed to get the project appx map contents.");
                return false;
            }

            using (var mapFileStream = File.Create(mapFile))
            {
                using (var mapFileWriter = new StreamWriter(mapFileStream))
                {
                    mapFileWriter.WriteLine("[ResourceMetadata]");
                    foreach (var md in resourceMetadata)
                    {
                        mapFileWriter.WriteLine(md);
                    }
                    mapFileWriter.WriteLine("");
                    mapFileWriter.WriteLine("[Files]");
                    foreach (var appxFile in appxFiles)
                    {
                        mapFileWriter.WriteLine(appxFile);
                    }
                }
            }
            OutputMessage(String.Format("... APPX map file created: {0}", mapFile));
            return true;
        }

        private bool CallMakeAppx(String makeAppxCmd, String mapFile, String outputAppx)
        {
            String makeAppxArgsFormat = "pack /l /h sha256 /m \"{0}\" /f \"{1}\" /o /p \"{2}\"";
            String makeAppxArgs = String.Format(makeAppxArgsFormat, outputFolder + @"\AppxManifest.xml", mapFile, outputAppx);
            String makeAppxLogfile = outputFolder + @"\makeappx.log";

            ExecuteExternalProcess(makeAppxCmd, makeAppxArgs, makeAppxLogfile);
            if (!File.Exists(outputAppx))
            {
                return false;
            }

            OutputMessage("... APPX file created");
            OutputMessage(String.Format("        {0}", outputAppx));
            OutputMessage(String.Format("        logfile: {0}", makeAppxLogfile));
            return true;
        }

        private bool SignAppx(String signToolCmd, String outputAppx, String pfxFile)
        {
            String signToolArgsFormat = "sign /fd sha256 /f \"{0}\" \"{1}\"";
            String signToolArgs = String.Format(signToolArgsFormat, pfxFile, outputAppx);
            String signToolLogfile = outputFolder + @"\signtool.log";

            ExecuteExternalProcess(signToolCmd, signToolArgs, signToolLogfile);
            // TODO: how to validate this?

            OutputMessage(String.Format("... APPX file signed with PFX", signToolLogfile));
            OutputMessage(String.Format("        logfile: {0}", signToolLogfile));
            return true;
        }

        private bool CreateCertFromPfx(String powershellCmd, String pfxFile, String outputCer)
        {
            String getCertArgsFormat = "\"Get-PfxCertificate -FilePath \'{0}\' | Export-Certificate -FilePath \'{1}\' -Type CERT\"";
            String getCertArgs = String.Format(getCertArgsFormat, pfxFile, outputCer);
            String powershellLogfile = outputFolder + @"\powershell.log";

            ExecuteExternalProcess(powershellCmd, getCertArgs, powershellLogfile);

            OutputMessage("... CER file generated from PFX");
            OutputMessage(String.Format("        {0}", outputCer));
            OutputMessage(String.Format("        logfile: {0}", powershellLogfile));
            return true;
        }

        private bool CopyDependencyAppxFiles(IProject project, List<FileStreamInfo> dependencies, String artifactsFolder)
        {
            foreach (var dependency in dependencies)
            {
                var success = dependency.Apply(artifactsFolder);
                if (!success)
                {
                    return false;
                }
            }
            OutputMessage("... dependencies copied into place");
            return true;
        }

        private bool CopyFileAndValidate(String from, String to)
        {
            File.Copy(from, to, true);
            if (!File.Exists(to))
            {
                Debug.WriteLine(String.Format("Failed to copy {0} to {1}", from, to));
                return false;
            }
            return true;
        }

        private bool CopyArtifacts(String outputAppx, String appxFilename, String outputCer, String cerFilename, List<FileStreamInfo> dependencies)
        {
            // If copy is not requested, skip
            if (null == copyOutputToFolder)
            {
                return true;
            }

            if (!Directory.Exists(copyOutputToFolder))
            {
                Directory.CreateDirectory(copyOutputToFolder);
            }

            // Copy APPX
            var success = CopyFileAndValidate(outputAppx, copyOutputToFolder + @"\" + appxFilename);
            if (!success)
            {
                return false;
            }
            // Copy .cer
            success = CopyFileAndValidate(outputCer, copyOutputToFolder + @"\" + cerFilename);
            if (!success)
            {
                return false;
            }
            // Copy dependencies
            foreach (var dependency in dependencies)
            {
                success = dependency.Apply(copyOutputToFolder);
                if (!success)
                {
                    Debug.WriteLine(String.Format("Failed to copy dependency to {0}", copyOutputToFolder + "\\" + dependency.AppxRelativePath));
                    return false;
                }
            }
            return true;
        }

        private async Task<bool> DeployAppx(String outputAppx, String outputCer, List<FileStreamInfo> dependencies, String dependencyFolder, String identityName)
        {
            // Create list of all APPX and CER files for deployment
            var files = new List<FileInfo>();
            files.Add(new FileInfo(outputAppx));
            files.Add(new FileInfo(outputCer));
            foreach (var dependency in dependencies)
            {
                files.Add(new FileInfo(dependencyFolder + @"\" + dependency.AppxRelativePath));
            }

            // Call WEBB Rest APIs to deploy
            var packageFullName = String.Format(packageFullNameFormat, identityName, targetType.ToString());
            var webbHelper = new WebbHelper();
            OutputMessage("... starting to deploy certificate, APPX, and dependencies");
            // Attempt to uninstall existing package if found
            var result = await webbHelper.UninstallAppAsync(packageFullName, targetName, credentials);
            if (result == HttpStatusCode.OK)
            {
                // result == OK means the package was uninstalled.
                OutputMessage(String.Format("... previously deployed {0} uninstalled successfully", packageFullName));
            }
            else
            {
                // result != OK could mean that the package wasn't already installed
                //           or it could mean that there was a problem with the uninstall
                //           request.
                OutputMessage(String.Format("... previous installation {0} was not uninstalled (if it wasn't previously installed, this is expected)", packageFullName));
            }
            // Deploy new APPX, cert, and dependency files
            result = await webbHelper.DeployAppAsync(files, targetName, credentials);
            if (result == HttpStatusCode.Accepted)
            {
                await webbHelper.PollInstallStateAsync(targetName, credentials);
                OutputMessage(String.Format("... deployment {0} finished.", packageFullName));

                OutputMessage("\r\n\r\n***");
                OutputMessage(String.Format("*** PackageFullName = {0}", packageFullName));
                OutputMessage("***\r\n\r\n");
                return true;
            }
            else
            {
                OutputMessage(String.Format("... deployment {0} failed.", packageFullName));
                return false;
            }
        }

        private async Task<bool> CreateAppx(ITemplate template, IProject project, String makeAppxCmd, String outputAppx)
        {
            // Copy generic base template files
            bool success = CopyBaseTemplateContents(template);
            if (!success)
            {
                return false;
            }

            // Copy IProject-specific (but still generic) files
            success = CopyProjectFiles(project);
            if (!success)
            {
                return false;
            }

            // Make changes to the files to tailor them to the specific user input
            success = SpecializeAppxManifest(project);
            if (!success)
            {
                return false;
            }

            // Do build step if needed (compiling/generation/etc)
            success = await BuildProjectAsync(project);
            if (!success)
            {
                return false;
            }

            // Add IProject-specific capabilities
            success = AddCapabilitiesToAppxManifest(project);
            if (!success)
            {
                return false;
            }

            // Create mapping file used to build APPX
            var mapFile = outputFolder + @"\main.map.txt";
            success = CreateAppxMapFile(template, project, mapFile);
            if (!success)
            {
                return false;
            }

            // Create APPX file
            success = CallMakeAppx(makeAppxCmd, mapFile, outputAppx);
            if (!success)
            {
                return false;
            }
            return true;
        }

        private async Task<bool> CreateAndDeployApp()
        {
            #region Find Template and Project from available providers

            // Ensure that the required Tools (MakeAppx and SignTool) can be found
            var universalSdkRoot = Registry.GetValue(universalSdkRootKey, universalSdkRootValue, null) as String;
            if (universalSdkRoot == null && (makeAppxPath == null || signToolPath == null))
            {
                NotifyThatMakeAppxOrSignToolNotFound();
                return false;
            }

            String sdkToolCmdFormat = "{0}\\bin\\{1}\\{2}";
            bool is64 = Environment.Is64BitOperatingSystem;
            String makeAppxCmd = (makeAppxPath == null) ?
                String.Format(sdkToolCmdFormat, universalSdkRoot, is64 ? "x64" : "x86", "MakeAppx.exe") :
                makeAppxPath;
            String signToolCmd = (signToolPath == null) ?
                String.Format(sdkToolCmdFormat, universalSdkRoot, is64 ? "x64" : "x86", "SignTool.exe") :
                signToolPath;
            if (!File.Exists(makeAppxCmd) || !File.Exists(signToolCmd))
            {
                NotifyThatMakeAppxOrSignToolNotFound();
                return false;
            }

            // Ensure that PowerShell.exe can be found
            var powershellCmd = (powershellPath == null) ?
                Registry.GetValue(powershellRootKey, powershellRootValue, null) as String :
                powershellPath;
            if (powershellCmd == null || !File.Exists(powershellCmd))
            {
                OutputMessage("Error: PowerShell.exe cannot be found.  Please use -powershell to provide");
                OutputMessage("       the location.");
                return false;
            }

            // Surround tool cmd paths with quotes in case there are spaces in the paths
            makeAppxCmd = "\"" + makeAppxCmd + "\"";
            signToolCmd = "\"" + signToolCmd + "\"";
            powershellCmd = "\"" + powershellCmd + "\"";

            // Find an appropriate path for the input source
            var supportedProjects = new SupportedProjects();
            IProject project = supportedProjects.FindProject(source);
            if (null == project)
            {
                OutputMessage(String.Format("Error: source is not supported. {0}", source));
                return false;
            }
            OutputMessage(String.Format("... project system found: {0}", project.Name));

            // Configure IProject with user input
            project.SourceInput = source;
            project.ProcessorArchitecture = targetType;
            project.SdkVersion = sdk;
            project.DependencyConfiguration = configuration;

            // Find base project type ... typically, this is C++ for non-standard UWP
            // project types like Python and Node.js
            IBaseProjectTypes baseProjectType = project.GetBaseProjectType();
            if (IBaseProjectTypes.Other == baseProjectType)
            {
                OutputMessage(String.Format("Error: base project type is not supported. {0}", baseProjectType.ToString()));
                return false;
            }

            // Get ITemplate to retrieve shared APPX content
            ITemplate template = supportedProjects.FindTemplate(baseProjectType);
            if (null == template)
            {
                OutputMessage(String.Format("Error: base project type is not supported. {0}", baseProjectType.ToString()));
                return false;
            }
            OutputMessage(String.Format("... base project system found: {0}", template.Name));

            #endregion

            outputFolder = Path.GetTempPath() + Path.GetRandomFileName();

            String artifactsFolder = outputFolder + @"\output";
            String filename = project.IdentityName + "_" + targetType + "_" + configuration;
            String appxFilename = filename + ".appx";
            String cerFilename = filename + ".cer";
            String outputAppx = artifactsFolder + @"\" + appxFilename;
            String outputCer = artifactsFolder + @"\" + cerFilename;

            Directory.CreateDirectory(artifactsFolder);

            var success = await CreateAppx(template, project, makeAppxCmd, outputAppx);
            if (!success)
            {
                return false;
            }

            String pfxFile = outputFolder + @"\TemporaryKey.pfx";
            success = SignAppx(signToolCmd, outputAppx, pfxFile);
            if (!success)
            {
                return false;
            }

            success = CreateCertFromPfx(powershellCmd, pfxFile, outputCer);
            if (!success)
            {
                return false;
            }

            var dependencies = project.GetDependencies(supportedProjects.DependencyProviders);
            success = CopyDependencyAppxFiles(project, dependencies, artifactsFolder);
            if (!success)
            {
                return false;
            }

            success = await DeployAppx(outputAppx, outputCer, dependencies, artifactsFolder, project.IdentityName);
            if (!success)
            {
                return false;
            }

            success = CopyArtifacts(outputAppx, appxFilename, outputCer, cerFilename, dependencies);
            return success;
        }

        DeploymentWorker(Stream outputStream)
        {
            this.outputWriter = new StreamWriter(outputStream);
            this.outputWriter.AutoFlush = true;
        }

        ~DeploymentWorker()
        {
            #region Cleanup

            if (!keepTempFolder && Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            #endregion
        }

        public static async Task<bool> Execute(string[] args, Stream outputStream)
        {
            DeploymentWorker worker = new DeploymentWorker(outputStream);
            var result2 = worker.CommandLineParser.Parse(args);
            var unrecognizedOptions = result2.AdditionalOptionsFound.GetEnumerator();
            var hasUnrecognizedOptions = unrecognizedOptions.MoveNext();
            var returnEarly = result2.HasErrors || hasUnrecognizedOptions || result2.HelpCalled;
            var showUsage = returnEarly && !result2.HelpCalled;
            if (showUsage)
            {
                if (hasUnrecognizedOptions)
                {
                    worker.OutputMessage("");
                    worker.OutputMessage("Error: Unrecognized options specified:");
                    worker.OutputMessage("");
                    do
                    {
                        worker.OutputMessage(String.Format("    -{0} {1}", unrecognizedOptions.Current.Key, unrecognizedOptions.Current.Value));
                    }
                    while (unrecognizedOptions.MoveNext());

                    worker.OutputMessage("");
                    worker.OutputMessage("Usage:");
                }

                worker.CommandLineParser.HelpOption.ShowHelp(worker.CommandLineParser.Options);
            }
            if (returnEarly)
            { 
                return false;
            }

            worker.OutputMessage("Starting utility to deploy an Iot Core app based on source ...");
            bool ret = await worker.CreateAndDeployApp();

            return ret;
        }
    }
}
