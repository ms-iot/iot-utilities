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

namespace IotCoreAppDeployment
{
    public class DeploymentWorker
    {
        #region Define arguments
        private class ArgumentHelper
        {
            public Regex ArgumentMatcher { get; set; }
            public Func<DeploymentWorker, String[], int, int> Handler { get; set; }
            public String HelpString { get; set; }

            public ArgumentHelper(String argName, String helpMsg)
            {
                ArgumentMatcher = new Regex(@"(^-" + argName + ")", RegexOptions.IgnoreCase);
                HelpString = "-" + argName + " " + helpMsg;
            }
            public ArgumentHelper(String[] argNames, String helpMsg)
            {
                StringBuilder args = new StringBuilder();
                for (int i = 0; i<argNames.Length; i++)
                {
                    if (i != 0) args.Append("|");
                    args.Append(argNames[i]);
                }
                ArgumentMatcher = new Regex(@"(^" + args.ToString() + ")", RegexOptions.IgnoreCase);
                HelpString = args.ToString() + " " + helpMsg;
            }
        }

        private List<ArgumentHelper> argumentHelper = new List<ArgumentHelper>() {
            new ArgumentHelper("source", "(source input)") {
                Handler = (worker, args, index) => { worker.source = args[index + 1]; return index+2; },
            },
            new ArgumentHelper("sdk", "(SDK version) {if nothing is provided, 10.0.10586.0 is the default}") {
                Handler = (worker, args, index) => {
                    switch (args[index+1])
                    {
                        case "10.0.10586.0": worker.sdk = SdkVersion.SDK_10_10586_0; break;
                        default:
                            worker.OutputMessage("Error: suported dependency sdks are: 10.0.10586.0");
                            return -1;
                    }
                    return index+2;
                },
            },
            new ArgumentHelper("config", "[Debug|Release] {if nothing is provided, Debug is the default}") {
                Handler = (worker, args, index) => {
                    switch (args[index+1])
                    {
                        case "DEBUG": worker.configuration = DependencyConfiguration.Debug; break;
                        case "RELEASE": worker.configuration = DependencyConfiguration.Release; break;
                        default:
                            worker.OutputMessage("Error: suported dependency configurations are: Debug | Release");
                            return -1;
                    }
                    return index+2;
                },
            },
            new ArgumentHelper("targetname", "(IoT Core device name or IP address)") {
                Handler = (worker, args, index) => { worker.targetName = args[index+1]; return index+2; },
            },
            new ArgumentHelper("targettype", "[ARM|X86] {if nothing is provided, ARM is the default}") {
                Handler = (worker, args, index) => {
                    switch (args[index+1])
                    {
                        case "arm": worker.targetType = TargetPlatform.ARM; break;
                        case "x86": worker.targetType = TargetPlatform.X86; break;
                        default:
                            worker.OutputMessage("Error: suported target types are ARM or X86.");
                            return -1;
                    }
                    return index+2;
                },
            },
            new ArgumentHelper("targetuser", "(IoT Core device username) {if nothing is provided, DefaultAccount is the default}") {
                Handler = (worker, args, index) => { worker.credentials.UserName = args[index+1]; return index+2; },
            },
            new ArgumentHelper("targetpassword", "(IoT Core device user password) {if nothing is provided, p@ssw0rd is the default}") {
                Handler = (worker, args, index) => { worker.credentials.Password = args[index+1]; return index+2; },
            },
            new ArgumentHelper("makeappx", "(MakeAppx.exe full path) {if nothing is provided, default Windows SDK installation assumed}") {
                Handler = (worker, args, index) => { worker.makeAppxPath = args[index+1]; return index+2; },
            },
            new ArgumentHelper("signtool", "(SignTool.exe full path) {if nothing is provided, default Windows SDK installation assumed}") {
                Handler = (worker, args, index) => { worker.signToolPath = args[index+1]; return index+2; },
            },
            new ArgumentHelper("powershell", "(PowerShell.exe full path) {if nothing is provided, the registry is queried}") {
                Handler = (worker, args, index) => { worker.powershellPath = args[index+1]; return index+2; },
            },
            new ArgumentHelper("output", "(full path to output APPX to) {if nothing is provided, files will not be saved}") {
                Handler = (worker, args, index) => { worker.copyOutputToFolder = args[index+1]; return index+2; },
            },
            new ArgumentHelper(new string[] { "-help", "-?", "-h" }, "Display usage") {
                Handler = (worker, args, index) => { worker.doUsage = true; return -1; },
            },
        };
        #endregion

        private String outputFolder = "";
        private String source = "";
        private String targetName = "";
        private String makeAppxPath = null;
        private String signToolPath = null;
        private String powershellPath = null;
        private String copyOutputToFolder = null;
        private TargetPlatform targetType = TargetPlatform.ARM;
        private SdkVersion sdk = SdkVersion.SDK_10_10586_0;
        private DependencyConfiguration configuration = DependencyConfiguration.Debug;

        private StreamWriter outputWriter;

        private const String universalSdkRootKey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows Kits\Installed Roots";
        private const String universalSdkRootValue = @"KitsRoot10";

        private const String powershellRootKey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell";
        private const String powershellRootValue = @"Path";

        private UserInfo credentials = new UserInfo() { UserName = "Administrator", Password = "p@ssw0rd" };
        private const int QueryInterval = 3000;

        bool doUsage = false;

        private enum RETURN_CODE
        {
            SUCCESS = 0,
            ERROR_UNKNOWN,
            ERROR_BAD_ARGUMENT,
        };

        RETURN_CODE ParseCommandLine(String[] argv)
        {

	        if (argv.Length <= 1)
	        {
		        doUsage = true;
		        return RETURN_CODE.ERROR_BAD_ARGUMENT;
	        }

            doUsage = false;
            int current = 0;
	        while (current < argv.Length)
	        {
                foreach (var helper in argumentHelper)
                {
                    if (helper.ArgumentMatcher.IsMatch(argv[current]))
                    {
                        current = helper.Handler(this, argv, current);
                        if (current == -1)
                        {
                            return RETURN_CODE.ERROR_BAD_ARGUMENT;
                        }
                        break;
                    }
                }
	        }
	        return RETURN_CODE.SUCCESS;
        }

        public void OutputMessage(String message)
        {
            outputWriter.WriteLine(message);
        }

        void DoUsage()
        {
            String appName = "IotCoreAppDeployment.exe";
            OutputMessage("Usage:");
            OutputMessage(String.Format("  {0} arguments:", appName));
            foreach (var helper in argumentHelper)
            {
                OutputMessage("    " + helper.HelpString);
            }
            OutputMessage("");
            OutputMessage("Example:");
            OutputMessage(String.Format("  {0} s -source app.py -targetname 1.2.3.4 -targettype ARM -sdk 10.0.10586.0", appName));
            OutputMessage("");
        }

        void ExecuteExternalProcess(String executableFileName, String arguments, String logFileName)
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
                    logStream.WriteLine("Errors:");
                    logStream.Write(errors);
                }
                logStream.WriteLine("\n\n\n\nFull Output:");
                logStream.Write(output.ToString());
            }
        }

        async Task<bool> CreateAndDeployApp()
        {
            #region Find Template and Project from available providers

            // Ensure that the required Tools (MakeAppx and SignTool) can be found
            var universalSdkRoot = Registry.GetValue(universalSdkRootKey, universalSdkRootValue, null) as String;
            if (universalSdkRoot == null && (makeAppxPath == null || signToolPath == null))
            {
                OutputMessage("Error: MakeAppx.exe and SignTool.exe must be installed.  These tools ");
                OutputMessage("       are installed as part of the Windows Standalone SDK for Windows 10 ");
                OutputMessage("       (https://go.microsoft.com/fwlink/?LinkID=698771).  If they are ");
                OutputMessage("       present on your machine, please provide the paths using -makeappx ");
                OutputMessage("       and -signtool.");
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
                OutputMessage("Error: MakeAppx.exe and SignTool.exe must be installed.  These tools ");
                OutputMessage("       are installed as part of the Windows Standalone SDK for Windows 10 ");
                OutputMessage("       (https://go.microsoft.com/fwlink/?LinkID=698771).  If they are ");
                OutputMessage("       present on your machine, please provide the paths using -makeappx ");
                OutputMessage("       and -signtool.");
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

            #region Create APPX
            outputFolder = Path.GetTempPath() + Path.GetRandomFileName();

            String artifactsFolder = outputFolder + @"\output";
            String appxFilename = project.IdentityName + ".appx";
            String cerFilename = project.IdentityName + ".cer";
            String outputAppx = artifactsFolder + @"\" + appxFilename;
            String outputCer = artifactsFolder + @"\" + cerFilename;

            Directory.CreateDirectory(artifactsFolder);

            // 1. Copy generic base template files
            #region Set up base template contents
            var templateContents = template.GetTemplateContents();
            foreach (var content in templateContents)
            {
                content.Apply(outputFolder);
            }
            OutputMessage(String.Format("... base project files found and copied: {0}", outputFolder));
            #endregion

            // 2. Copy IProject-specific (but still generic) files
            #region Add project specific content
            var appxContents = project.GetAppxContents();
            foreach (var content in appxContents)
            {
                content.Apply(outputFolder);
            }
            OutputMessage(String.Format("... project files found and copied: {0}", outputFolder));
            #endregion

            // 3. Make changes to the files to tailor them to the specific user input
            #region Make changes to generic project files
            var appxManifestChangess = project.GetAppxContentChanges();
            foreach (var change in appxManifestChangess)
            {
                change.ApplyToContent(outputFolder);
            }
            OutputMessage("... project files tailored to current deployment.");
            #endregion

            // 4. Add IProject-specific capabilities
            #region Add capabilities
            var capabilityAdditions = project.GetCapabilities();
            foreach (var capability in capabilityAdditions)
            {
                capability.ApplyToContent(outputFolder);
            }
            #endregion

            // 5. Create mapping file used to build APPX
            #region Create APPX map file
            var mapFile = outputFolder + @"\main.map.txt";
            var resourceMetadata = new List<String>();
            var appxFiles = new List<String>();
            template.GetAppxMapContents(resourceMetadata, appxFiles, outputFolder);
            project.GetAppxMapContents(resourceMetadata, appxFiles, outputFolder);
            var mapFileStream = File.Create(mapFile);
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
            OutputMessage(String.Format("... APPX map file created: {0}", mapFile));
            #endregion

            // 6. Create APPX file
            #region Call MakeAppx.exe
            String makeAppxArgsFormat = "pack /l /h sha256 /m \"{0}\" /f \"{1}\" /o /p \"{2}\"";
            String makeAppxArgs = String.Format(makeAppxArgsFormat, outputFolder + @"\AppxManifest.xml", mapFile, outputAppx);
            String makeAppxLogfile = outputFolder + @"\makeappx.log";

            ExecuteExternalProcess(makeAppxCmd, makeAppxArgs, makeAppxLogfile);

            OutputMessage("... APPX file created");
            OutputMessage(String.Format("        {0}", outputAppx));
            OutputMessage(String.Format("        logfile: {0}", makeAppxLogfile));

            #endregion

            // 7. Sign APPX file using shared PFX
            #region Call SignTool.exe
            String pfxFile = outputFolder + @"\TemporaryKey.pfx";
            String signToolArgsFormat = "sign /fd sha256 /f \"{0}\" \"{1}\"";
            String signToolArgs = String.Format(signToolArgsFormat, pfxFile, outputAppx);
            String signToolLogfile = outputFolder + @"\signtool.log";

            ExecuteExternalProcess(signToolCmd, signToolArgs, signToolLogfile);

            OutputMessage(String.Format("... APPX file signed with PFX", signToolLogfile));
            OutputMessage(String.Format("        logfile: {0}", signToolLogfile));

            #endregion

            // 8. Get CER file from shared PFX
            #region Create CER file from PFX
            String getCertArgsFormat = "\"Get-PfxCertificate -FilePath \'{0}\' | Export-Certificate -FilePath \'{1}\' -Type CERT\"";
            String getCertArgs = String.Format(getCertArgsFormat, pfxFile, outputCer);
            String powershellLogfile = outputFolder + @"\powershell.log";

            ExecuteExternalProcess(powershellCmd, getCertArgs, powershellLogfile);

            OutputMessage("... CER file generated from PFX");
            OutputMessage(String.Format("        {0}", outputCer));
            OutputMessage(String.Format("        logfile: {0}", powershellLogfile));
            #endregion

            // 9. Copy appropriate Dependencies from IProject
            #region Gather Dependencies

            var dependencies = project.GetDependencies(supportedProjects.DependencyProviders);
            foreach (var dependency in dependencies)
            {
                dependency.Apply(artifactsFolder);
            }
            OutputMessage("... dependencies copied into place");

            #endregion

            #endregion

            #region Deploy APPX

            #region Create list of all APPX and CER files for deployment
            var files = new List<FileInfo>();
            files.Add(new FileInfo(outputAppx));
            files.Add(new FileInfo(outputCer));
            foreach (var dependency in dependencies)
            {
                files.Add(new FileInfo(artifactsFolder + @"\" + dependency.AppxRelativePath));
            }
            #endregion

            #region Call WEBB Rest APIs to deploy
            var webbHelper = new WebbHelper();
            OutputMessage("... Starting to deploy certificate, APPX, and dependencies");
            var result = await webbHelper.DeployAppAsync(files, targetName, credentials);
            if (result == HttpStatusCode.Accepted)
            {
                await webbHelper.PollInstallStateAsync(targetName, credentials);
            }
            else
            {
                OutputMessage("... Deployment failed.");
                return false;
            }
            OutputMessage("... Deployment finished.");
            #endregion

            #endregion

            #region Copy artifacts if requested

            if (null != copyOutputToFolder)
            {
                if (Directory.Exists(copyOutputToFolder))
                {
                    Directory.Delete(copyOutputToFolder, true);
                }
                Directory.CreateDirectory(copyOutputToFolder);
                File.Copy(outputAppx, copyOutputToFolder + @"\" + appxFilename);
                File.Copy(outputCer, copyOutputToFolder + @"\" + cerFilename);
                foreach (var dependency in dependencies)
                {
                    dependency.Apply(copyOutputToFolder);
                }
            }

            #endregion


            OutputMessage("\r\n\r\n***");
            OutputMessage(String.Format("*** PackageFullName = {0}_1.0.0.0_{1}__1w720vyc4ccym", project.IdentityName, configuration.ToString()));
            OutputMessage("***\r\n\r\n");

            return true;
        }

        DeploymentWorker(Stream outputStream)
        {
            this.outputWriter = new StreamWriter(outputStream);
            this.outputWriter.AutoFlush = true;
        }

        ~DeploymentWorker()
        {
            #region Cleanup

            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            #endregion
        }

        public static async Task<bool> Execute(string[] args, Stream outputStream)
        {
            DeploymentWorker worker = new DeploymentWorker(outputStream);
            var result = worker.ParseCommandLine(args);
            if (worker.doUsage) { worker.DoUsage(); return false; }

            worker.OutputMessage("Starting utility to deploy an Iot Core app based on source ...");
            bool ret = await worker.CreateAndDeployApp();
            worker.OutputMessage("... finished");

            return ret;
        }
    }
}
