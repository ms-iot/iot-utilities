// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using System.IO.Compression;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Iot.Ino
{
    public class InoProject : IProjectWithCustomBuild
    {
        public string Name => "Arduino Wiring Project";
        public string IdentityName => "ino-uwp";

        public bool IsSourceSupported(string source)
        {
            if (source != null)
            {
                return source.EndsWith(".ino", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public IBaseProjectTypes GetBaseProjectType()
        {
            return IBaseProjectTypes.CPlusPlusBackgroundApplication;
        }

        public TargetPlatform ProcessorArchitecture { set; get; }
        public SdkVersion SdkVersion { set; get; }
        private string SdkVersionString
        {
            get
            {
                switch (SdkVersion)
                {
                    case SdkVersion.SDK_10_0_10586_0: return "10.0.10586.0";
                    default:
                        return "10.0.10240.0";
                }
            }
        }
        public DependencyConfiguration DependencyConfiguration { set; get; }
        public string SourceInput { set; get; }

        private static string IdentityPublisher => "CN=" + PropertiesPublisherDisplayName;
        private static string PropertiesPublisherDisplayName => "MSFT";

        private string _PhoneIdentityGuid = null;
        private string PhoneIdentityGuid
        {
            get
            {
                if (_PhoneIdentityGuid == null)
                {
                    _PhoneIdentityGuid = Guid.NewGuid().ToString();
                }
                return _PhoneIdentityGuid;
            }
        }
        private static string PropertiesDisplayName => "InoBackgroundApplication1";

        private static string DisplayName => "inouwp";
        private static string Description => "inouwp";
        private static string ExtensionEntryPoint => "ArduinoWiringApplication.StartupTask";
        private static string InProcessServerPath => PropertiesDisplayName + ".dll";
        private static string InProcessServerActivatableClassId => "ArduinoWiringApplication.StartupTask";

        private static string _SdkRoot = null;
        private static string SdkRoot
        {
            get
            {
                if (_SdkRoot == null)
                {
                    const string universalSdkRootKey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows Kits\Installed Roots";
                    const string universalSdkRootValue = @"KitsRoot10";
                    _SdkRoot = Registry.GetValue(universalSdkRootKey, universalSdkRootValue, null) as string;
                }
                return _SdkRoot;
            }
        }

        private static string _RegistryVcCompilerPath = null;
        private static string RegistryVcCompilerPath
        {
            get
            {
                if (_RegistryVcCompilerPath == null)
                {
                    _RegistryVcCompilerPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\VisualStudio\VC\19.0\x86\x86", "Compiler", "") as string;
                }
                return _RegistryVcCompilerPath;
            }
        }

        private static string _VCToolsWorkingDirectory = null;
        private static string VCToolsWorkingDirectoryFromRegistry
        {
            get
            {
                if (_VCToolsWorkingDirectory == null)
                {
                    _VCToolsWorkingDirectory = Path.GetDirectoryName(RegistryVcCompilerPath);
                }
                return _VCToolsWorkingDirectory;
            }
        }

        private string _CompilerPath = null;
        private string CompilerPathFromRegistry
        {
            get
            {
                if (_CompilerPath == null)
                {
                    string compilerFolder = Path.GetDirectoryName(RegistryVcCompilerPath);
                    _CompilerPath = compilerFolder +
                        ((ProcessorArchitecture == TargetPlatform.ARM) ?
                            @"\x86_arm\cl.exe" :
                            @"\cl.exe");
                }
                return _CompilerPath;
            }
        }

        private static string _LinkerPath = null;
        private static string LinkerPathFromRegistry
        {
            get
            {
                if (_LinkerPath == null)
                {
                    string compilerFolder = Path.GetDirectoryName(RegistryVcCompilerPath);
                    _LinkerPath = compilerFolder + @"\link.exe";
                }
                return _LinkerPath;
            }
        }

        private static string _VCLibPath = null;
        private static string VCLibPath
        {
            get
            {
                if (_VCLibPath == null)
                {
                    string vsCommonToolsPath = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
                    if (vsCommonToolsPath != null)
                    {
                        _VCLibPath = new FileInfo(vsCommonToolsPath += "\\..\\..\\VC\\lib").FullName;
                    }
                }
                return _VCLibPath;
            }
        }

        private static string _VCIncludePath = null;
        private static string VCIncludePath
        {
            get
            {
                if (_VCIncludePath == null)
                {
                    string vsCommonToolsPath = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
                    if (vsCommonToolsPath != null)
                    {
                        _VCIncludePath = new FileInfo(vsCommonToolsPath += "\\..\\..\\VC\\include").FullName;
                    }
                }
                return _VCIncludePath;
            }
        }

        private static string _IotCoreAppDeploymentCache = null;
        private static string IotCoreAppDeploymentCache
        {
            get
            {
                if (_IotCoreAppDeploymentCache == null)
                {
                    _IotCoreAppDeploymentCache =
                        new FileInfo(Path.GetTempPath() + "IotCoreAppDeploymentCache").FullName;
                }
                return _IotCoreAppDeploymentCache;
            }
        }

        private static string _InoVersion = null;
        private static string InoVersion
        {
            get
            {
                if (_InoVersion == null)
                {
                    Assembly assembly = Assembly.GetAssembly(typeof(InoProject));
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                    _InoVersion = fileVersionInfo.ProductVersion;
                }
                return _InoVersion;
            }
        }

        public ReadOnlyCollection<IContentChange> GetAppxContentChanges()
        {
            var sdkVersionString = SdkVersionString;
            var changes = new List<IContentChange>() {
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Identity/@Name", Value = IdentityName },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Identity/@Publisher", Value = IdentityPublisher },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Identity/@ProcessorArchitecture", Value = ProcessorArchitecture.ToString().ToLower(CultureInfo.InvariantCulture) },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/mp:PhoneIdentity/@PhoneProductId", Value = PhoneIdentityGuid },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Properties/std:DisplayName", IsAttribute = false, Value = PropertiesDisplayName },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Properties/std:PublisherDisplayName", IsAttribute = false, Value = PropertiesPublisherDisplayName },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Dependencies/std:TargetDeviceFamily/@MinVersion", Value = sdkVersionString },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Dependencies/std:TargetDeviceFamily/@MaxVersionTested", Value = sdkVersionString },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Applications/std:Application/uap:VisualElements/@DisplayName", Value = DisplayName },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Applications/std:Application/uap:VisualElements/@Description", Value = Description },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Applications/std:Application/std:Extensions/std:Extension/@EntryPoint", Value = ExtensionEntryPoint },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Extensions/std:Extension/std:InProcessServer/std:Path", IsAttribute = false, Value = InProcessServerPath },
                        new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Extensions/std:Extension/std:InProcessServer/std:ActivatableClass/@ActivatableClassId", Value = InProcessServerActivatableClassId },
                    };
            return new ReadOnlyCollection<IContentChange>(changes);
        }

        public ReadOnlyCollection<IContentChange> GetCapabilities()
        {
            var changes = new Collection<IContentChange>()
                    {
                        new AppxManifestCapabilityAddition() { CapabilityName = "internetClientServer" },
                        new AppxManifestCapabilityAddition() { CapabilityName = "lowLevelDevices", CapabilityNamespace = "iot" },
                        new AppxManifestCapabilityAddition() { Capability = "DeviceCapability", CapabilityName = "109b86ad-f53d-4b76-aa5f-821e2ddf2141" },
                    };
            return new ReadOnlyCollection<IContentChange>(changes);
        }

        public ReadOnlyCollection<FileStreamInfo> GetAppxContents()
        {
            return new ReadOnlyCollection<FileStreamInfo>(new List<FileStreamInfo>());
        }

        public bool GetAppxMapContents(Collection<string> resourceMetadata, Collection<string> files, string outputFolder)
        {
            files.Add("\"" + outputFolder + "\\" + PropertiesDisplayName + ".dll\" \"" + PropertiesDisplayName + ".dll\"");
            files.Add("\"" + outputFolder + "\\" + PropertiesDisplayName + ".winmd\" \"" + PropertiesDisplayName + ".winmd\"");

            string versionedCache = IotCoreAppDeploymentCache + @"\" + InoVersion;
            string runtimePath = versionedCache + @"\lightning\runtimes\win10-" + ProcessorArchitecture + @"\native\";
            files.Add("\"" + runtimePath + "Lightning.dll\" \"Lightning.dll\"");
            files.Add("\"" + runtimePath + "Microsoft.IoT.Lightning.Providers.dll\" \"Microsoft.IoT.Lightning.Providers.dll\"");

            string winmdPath = versionedCache + @"\lightning\lib\uap10.0\";
            files.Add("\"" + winmdPath + "Microsoft.IoT.Lightning.Providers.winmd\" \"Microsoft.IoT.Lightning.Providers.winmd\"");

            return true;
        }

        public ReadOnlyCollection<FileStreamInfo> GetDependencies(Collection<IDependencyProvider> availableDependencyProviders)
        {
            foreach (var dependencyProvider in availableDependencyProviders)
            {
                var supportedDependencies = dependencyProvider.GetSupportedDependencies();
                if (supportedDependencies.ContainsKey("CPlusPlusUwp"))
                {
                    return supportedDependencies["CPlusPlusUwp"].GetDependencies(ProcessorArchitecture, DependencyConfiguration, SdkVersion);
                }
            }
            return new ReadOnlyCollection<FileStreamInfo>(new List<FileStreamInfo>());
        }

        private static void ExecuteExternalProcess(string executableFileName, string workingDirectory, string arguments, string logFileName)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = executableFileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = workingDirectory;
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

                using (var logStream = new StreamWriter(logFileName, true))
                {
                    logStream.Write("Command: ");
                    logStream.WriteLine(executableFileName);

                    logStream.WriteLine("Arguments: ");
                    logStream.WriteLine(arguments);

                    var errors = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(errors))
                    {
                        logStream.WriteLine("Errors:");
                        logStream.Write(errors);
                    }
                    logStream.WriteLine("\n\n\n\nFull Output:");
                    logStream.Write(output.ToString());
                }
            }
        }
        private bool CompileFile(string sourceFile, string sourceRoot, string cachedRoot, string fullBuildOutputDir, bool useCachedVersionIfAvailable)
        {
            return CompileFiles(new string[] { sourceFile }, sourceRoot, cachedRoot, fullBuildOutputDir, useCachedVersionIfAvailable);
        }

        private bool CompileFiles(string[] sourceFiles, string sourceRoot, string cachedRoot, string fullBuildOutputDir, bool useCachedVersionIfAvailable)
        {
            var filesToBuild = new StringBuilder();
            foreach (var sourceFile in sourceFiles)
            {
                var sourceFileInfo = new FileInfo(sourceFile);
                var objFilePath = fullBuildOutputDir + "\\" + sourceFileInfo.Name.Replace(sourceFileInfo.Extension, ".obj");
                if (useCachedVersionIfAvailable && File.Exists(objFilePath))
                {
                    continue;
                }

                filesToBuild.Append("\"" + sourceFile + "\" ");
            }

            if (filesToBuild.Length == 0)
            {
                // Everything is cached ... nothing to build!
                return true;
            }

            // Construct compiler arguments
            var compilerArgsBuilder = new StringBuilder();
            compilerArgsBuilder.Append("/c ");
            compilerArgsBuilder.Append("/showIncludes ");
            compilerArgsBuilder.Append("/I\"" + sourceRoot + "\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + cachedRoot + "\\lightning\\include\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + cachedRoot + "\\lightning\\include\\avr\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + VCIncludePath + "\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + SdkRoot + @"Include\" + SdkVersionString + "\\um\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + SdkRoot + @"Include\" + SdkVersionString + "\\shared\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + SdkRoot + @"Include\" + SdkVersionString + "\\ucrt\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + SdkRoot + @"Include\" + SdkVersionString + "\\winrt\\\\\" ");
            compilerArgsBuilder.Append("/I" + fullBuildOutputDir + " ");
            compilerArgsBuilder.Append("/Zi ");
            compilerArgsBuilder.Append("/ZW ");
            compilerArgsBuilder.Append("/ZW:nostdlib ");
            compilerArgsBuilder.Append("/nologo ");
            compilerArgsBuilder.Append("/W3 ");
            compilerArgsBuilder.Append("/WX- ");
            compilerArgsBuilder.Append("/sdl ");
            compilerArgsBuilder.Append("/Od ");
            compilerArgsBuilder.Append("/Oy- ");
            compilerArgsBuilder.Append("/D _WINRT_DLL ");
            compilerArgsBuilder.Append("/D _WIN_IOT ");
            compilerArgsBuilder.Append("/D _ARM_WINAPI_PARTITION_DESKTOP_SDK_AVAILABLE=1 ");
            compilerArgsBuilder.Append("/D _WINDLL ");
            compilerArgsBuilder.Append("/D _UNICODE ");
            compilerArgsBuilder.Append("/D UNICODE ");
            compilerArgsBuilder.Append("/D _DEBUG ");
            compilerArgsBuilder.Append("/D WINAPI_FAMILY=WINAPI_FAMILY_APP ");
            compilerArgsBuilder.Append("/D __WRL_NO_DEFAULT_LIB__ ");
            compilerArgsBuilder.Append("/Gm- ");
            compilerArgsBuilder.Append("/EHsc ");
            compilerArgsBuilder.Append("/RTC1 ");
            compilerArgsBuilder.Append("/MDd ");
            compilerArgsBuilder.Append("/GS ");
            compilerArgsBuilder.Append("/fp:precise ");
            compilerArgsBuilder.Append("/Zc:wchar_t ");
            compilerArgsBuilder.Append("/Zc:forScope ");
            compilerArgsBuilder.Append("/Zc:inline ");
            compilerArgsBuilder.Append("/Fo\"" + fullBuildOutputDir + "\\\" ");
            compilerArgsBuilder.Append("/Fd\"" + fullBuildOutputDir + "\\VC140.PDB\" ");
            compilerArgsBuilder.Append("/Gd ");
            compilerArgsBuilder.Append("/TP ");
            compilerArgsBuilder.Append("/FI\"" + sourceRoot + "\\pch.h\" ");

            compilerArgsBuilder.Append("/FU\"" + cachedRoot + "\\lightning\\lib\\uap10.0\\Microsoft.IoT.Lightning.Providers.winmd\" ");
            compilerArgsBuilder.Append("/FU\"" + VCLibPath + "\\STORE\\REFERENCES\\PLATFORM.WINMD\" ");

            var winmdFiles = System.IO.Directory.GetFiles(SdkRoot, "*.winmd", SearchOption.AllDirectories);
            var alreadyUsed = new HashSet<string>();
            alreadyUsed.Add("windows.applicationmodel.calls.callsphonecontract.winmd");
            alreadyUsed.Add("windows.management.orchestration.orchestrationcontract.winmd");
            foreach (var winmdPath in winmdFiles)
            {
                var winmdFile = Path.GetFileName(winmdPath).ToLower();
                if (!alreadyUsed.Contains(winmdFile) && !String.Equals("windows.winmd", winmdFile))
                {
                    compilerArgsBuilder.Append("/FU \"");
                    compilerArgsBuilder.Append(winmdPath);
                    compilerArgsBuilder.Append("\" ");
                    alreadyUsed.Add(winmdFile);
                }
            }
            /*
            var winmdReferenceFormat = "/FU\"" + SdkRoot + "REFERENCES\\{0}\\{1}\\{2}\" ";
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATEDEVENTSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATEDEVENTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATIONCAMERASETTINGSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATIONCAMERASETTINGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.CONTACTACTIVATEDEVENTSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.CONTACTACTIVATEDEVENTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.WEBUISEARCHACTIVATEDEVENTSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.WEBUISEARCHACTIVATEDEVENTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.BACKGROUND.BACKGROUNDALARMAPPLICATIONCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.BACKGROUND.BACKGROUNDALARMAPPLICATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.CALLS.BACKGROUND.CALLSBACKGROUNDCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.CALLS.BACKGROUND.CALLSBACKGROUNDCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.CALLS.LOCKSCREENCALLCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.CALLS.LOCKSCREENCALLCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.RESOURCES.MANAGEMENT.RESOURCEINDEXERCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.RESOURCES.MANAGEMENT.RESOURCEINDEXERCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.SEARCH.CORE.SEARCHCORECONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.SEARCH.CORE.SEARCHCORECONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.SEARCH.SEARCHCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.SEARCH.SEARCHCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.WALLET.WALLETCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.WALLET.WALLETCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.DEVICES.CUSTOM.CUSTOMDEVICECONTRACT", "1.0.0.0", "WINDOWS.DEVICES.CUSTOM.CUSTOMDEVICECONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.DEVICES.PORTABLE.PORTABLEDEVICECONTRACT", "1.0.0.0", "WINDOWS.DEVICES.PORTABLE.PORTABLEDEVICECONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.DEVICES.PRINTERS.EXTENSIONS.EXTENSIONSCONTRACT", "2.0.0.0", "WINDOWS.DEVICES.PRINTERS.EXTENSIONS.EXTENSIONSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.DEVICES.SCANNERS.SCANNERDEVICECONTRACT", "1.0.0.0", "WINDOWS.DEVICES.SCANNERS.SCANNERDEVICECONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.DEVICES.SMS.LEGACYSMSAPICONTRACT", "1.0.0.0", "WINDOWS.DEVICES.SMS.LEGACYSMSAPICONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.GAMING.PREVIEW.GAMESENUMERATIONCONTRACT", "1.0.0.0", "WINDOWS.GAMING.PREVIEW.GAMESENUMERATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.GLOBALIZATION.GLOBALIZATIONJAPANESEPHONETICANALYZERCONTRACT", "1.0.0.0", "WINDOWS.GLOBALIZATION.GLOBALIZATIONJAPANESEPHONETICANALYZERCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MANAGEMENT.DEPLOYMENT.PREVIEW.DEPLOYMENTPREVIEWCONTRACT", "1.0.0.0", "WINDOWS.MANAGEMENT.DEPLOYMENT.PREVIEW.DEPLOYMENTPREVIEWCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MANAGEMENT.ORCHESTRATION.ORCHESTRATIONCONTRACT", "1.0.0.0", "WINDOWS.MANAGEMENT.ORCHESTRATION.ORCHESTRATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MANAGEMENT.WORKPLACE.WORKPLACESETTINGSCONTRACT", "1.0.0.0", "WINDOWS.MANAGEMENT.WORKPLACE.WORKPLACESETTINGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MEDIA.CAPTURE.APPCAPTURECONTRACT", "2.0.0.0", "WINDOWS.MEDIA.CAPTURE.APPCAPTURECONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MEDIA.CAPTURE.CAMERACAPTUREUICONTRACT", "1.0.0.0", "WINDOWS.MEDIA.CAPTURE.CAMERACAPTUREUICONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MEDIA.DEVICES.CALLCONTROLCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.DEVICES.CALLCONTROLCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MEDIA.MEDIACONTROLCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.MEDIACONTROLCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MEDIA.PLAYLISTS.PLAYLISTSCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.PLAYLISTS.PLAYLISTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.MEDIA.PROTECTION.PROTECTIONRENEWALCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.PROTECTION.PROTECTIONRENEWALCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.NETWORKING.NETWORKOPERATORS.LEGACYNETWORKOPERATORSCONTRACT", "1.0.0.0", "WINDOWS.NETWORKING.NETWORKOPERATORS.LEGACYNETWORKOPERATORSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.NETWORKING.SOCKETS.CONTROLCHANNELTRIGGERCONTRACT", "1.0.0.0", "WINDOWS.NETWORKING.SOCKETS.CONTROLCHANNELTRIGGERCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SECURITY.ENTERPRISEDATA.ENTERPRISEDATACONTRACT", "2.0.0.0", "WINDOWS.SECURITY.ENTERPRISEDATA.ENTERPRISEDATACONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SECURITY.EXCHANGEACTIVESYNCPROVISIONING.EASCONTRACT", "1.0.0.0", "WINDOWS.SECURITY.EXCHANGEACTIVESYNCPROVISIONING.EASCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SERVICES.MAPS.GUIDANCECONTRACT", "2.0.0.0", "WINDOWS.SERVICES.MAPS.GUIDANCECONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SERVICES.MAPS.LOCALSEARCHCONTRACT", "2.0.0.0", "WINDOWS.SERVICES.MAPS.LOCALSEARCHCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SYSTEM.PROFILE.PROFILEHARDWARETOKENCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.PROFILE.PROFILEHARDWARETOKENCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SYSTEM.PROFILE.PROFILERETAILINFOCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.PROFILE.PROFILERETAILINFOCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SYSTEM.PROFILE.SYSTEMMANUFACTURERS.SYSTEMMANUFACTURERSCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.PROFILE.SYSTEMMANUFACTURERS.SYSTEMMANUFACTURERSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SYSTEM.USERPROFILE.USERPROFILECONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.USERPROFILE.USERPROFILECONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.SYSTEM.USERPROFILE.USERPROFILELOCKSCREENCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.USERPROFILE.USERPROFILELOCKSCREENCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.UI.APPLICATIONSETTINGS.APPLICATIONSSETTINGSCONTRACT", "1.0.0.0", "WINDOWS.UI.APPLICATIONSETTINGS.APPLICATIONSSETTINGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.UI.CORE.ANIMATIONMETRICS.ANIMATIONMETRICSCONTRACT", "1.0.0.0", "WINDOWS.UI.CORE.ANIMATIONMETRICS.ANIMATIONMETRICSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.UI.CORE.COREWINDOWDIALOGSCONTRACT", "1.0.0.0", "WINDOWS.UI.CORE.COREWINDOWDIALOGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.UI.XAML.HOSTING.HOSTINGCONTRACT", "1.0.0.0", "WINDOWS.UI.XAML.HOSTING.HOSTINGCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.WEB.HTTP.DIAGNOSTICS.HTTPDIAGNOSTICSCONTRACT", "1.0.0.0", "WINDOWS.WEB.HTTP.DIAGNOSTICS.HTTPDIAGNOSTICSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.CALLS.CALLSVOIPCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.CALLS.CALLSVOIPCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.DEVICES.PRINTERS.PRINTERSCONTRACT", "1.0.0.0", "WINDOWS.DEVICES.PRINTERS.PRINTERSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.FOUNDATION.FOUNDATIONCONTRACT", "1.0.0.0", "WINDOWS.FOUNDATION.FOUNDATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.FOUNDATION.UNIVERSALAPICONTRACT", "2.0.0.0", "WINDOWS.FOUNDATION.UNIVERSALAPICONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.GRAPHICS.PRINTING3D.PRINTING3DCONTRACT", "2.0.0.0", "WINDOWS.GRAPHICS.PRINTING3D.PRINTING3DCONTRACT.WINMD"));
            compilerArgsBuilder.Append(string.Format(CultureInfo.InvariantCulture, winmdReferenceFormat, "WINDOWS.NETWORKING.CONNECTIVITY.WWANCONTRACT", "1.0.0.0", "WINDOWS.NETWORKING.CONNECTIVITY.WWANCONTRACT.WINMD"));
            */
            compilerArgsBuilder.Append("/analyze- ");

            compilerArgsBuilder.Append(filesToBuild.ToString());

            // Compile the given files
            var compilerArgs = compilerArgsBuilder.ToString();
            ExecuteExternalProcess(CompilerPathFromRegistry, VCToolsWorkingDirectoryFromRegistry, compilerArgs, sourceRoot + "\\compile.log");

            // Check for the resulting obj file to determine success
            foreach (var sourceFile in sourceFiles)
            {
                var sourceFileInfo = new FileInfo(sourceFile);
                var extension = sourceFileInfo.Extension;
                var filename = sourceFileInfo.Name;

                var objFilename = filename.Substring(0, filename.Length - extension.Length) + ".obj";
                var objFilePath = fullBuildOutputDir + "\\" + objFilename;
                if (!File.Exists(objFilePath))
                {
                    Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_CompileFailed, sourceFile, objFilePath));
                    return false;
                }
            }
            return true;
        }

        private static bool CopyResourceFileBuildFolderAndUnzip(string resourceName, string outputFolder, string unzipFolder)
        {
            if (!CopyResourceFileBuildFolder(resourceName, outputFolder))
            {
                return false;
            }

            ZipFile.ExtractToDirectory(outputFolder + @"\" + resourceName, outputFolder + "\\" + unzipFolder);
            return Directory.Exists(outputFolder + "\\" + unzipFolder);
        }
        private static bool CopyResourceFileBuildFolder(string resourceName, string outputFolder)
        {
            var assemblyName = typeof(InoProject).Assembly.GetName().Name;
            var resource = string.Format(CultureInfo.InvariantCulture, @"{0}.Resources.{1}", assemblyName, resourceName);
            new FileStreamInfo()
            {
                AppxRelativePath = resourceName,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            }.Apply(outputFolder);

            return File.Exists(outputFolder + "\\" + resourceName);
        }

        private Task<bool> Compile(string outputFolder, StreamWriter logging)
        {
            if (!File.Exists(CompilerPathFromRegistry))
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_CompilerNotFound, CompilerPathFromRegistry));
                return Task.FromResult(false);
            }

            if (SdkRoot == null)
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_SdkNotFound));
                return Task.FromResult(false);
            }

            if (VCIncludePath == null)
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_VcIncludesNotFound));
                return Task.FromResult(false);
            }

            if (VCLibPath == null)
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_VcLibsNotFound));
                return Task.FromResult(false);
            }

            // Create build folder (i.e. ARM\Debug or X86\Debug)
            var buildOutputDir = ProcessorArchitecture + "\\" + DependencyConfiguration + "\\";
            var fullBuildOutputDir = outputFolder + "\\" + buildOutputDir;
            if (!Directory.Exists(fullBuildOutputDir))
            {
                Directory.CreateDirectory(fullBuildOutputDir);
            }

            // Get PCH.H from resources
            if (!CopyResourceFileBuildFolder("pch.h", outputFolder))
            {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_PchFailure));
                return Task.FromResult(false);
            }
            // Get PinNumbers.h from resources
            if (!CopyResourceFileBuildFolder("PinNumbers.h", outputFolder))
            {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_StartupTaskFailure));
                return Task.FromResult(false);
            }
            // Get StartupTask.cpp from resources
            if (!CopyResourceFileBuildFolder("StartupTask.cpp", outputFolder))
            {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_StartupTaskFailure));
                return Task.FromResult(false);
            }

            string versionedCache = IotCoreAppDeploymentCache + @"\" + InoVersion;
            string versionedConfigCache = versionedCache + @"\" + buildOutputDir;
            if (!Directory.Exists(versionedCache))
            {
                Directory.CreateDirectory(versionedCache);
            }
            if (!Directory.Exists(versionedConfigCache))
            {
                Directory.CreateDirectory(versionedConfigCache);
            }
            if (!Directory.Exists(versionedCache + "\\lightning"))
            {
                // Get lightning sources from resources
                if (!CopyResourceFileBuildFolderAndUnzip("microsoft.iot.lightning.1.1.0-alpha.nupkg", versionedCache, "lightning"))
                {
                    Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_LightningNupkgFailure));
                    return Task.FromResult(false);
                }
            }

            // Compile StartupTask.cpp
            if (!CompileFile(outputFolder + @"\StartupTask.cpp", outputFolder, versionedCache, fullBuildOutputDir, false))
            {
                logging.WriteLine(Resource.InoProject_CompilationFailure);
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_FileCompilationFailure, outputFolder + @"\StartupTask.cpp"));
                return Task.FromResult(false);
            }
            // Compile the users INO input file
            if (!CompileFile(SourceInput, outputFolder, versionedCache, fullBuildOutputDir, false))
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_FileCompilationFailure, SourceInput));
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> Link(string outputFolder, StreamWriter logging)
        {
            if (!File.Exists(LinkerPathFromRegistry))
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_LinkerNotFound, LinkerPathFromRegistry));
                return Task.FromResult(false);
            }

            if (SdkRoot == null)
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_SdkNotFound));
                return Task.FromResult(false);
            }

            if (VCLibPath == null)
            {
                logging.WriteLine(string.Format(CultureInfo.InvariantCulture, Resource.InoProject_VcLibsNotFound));
                return Task.FromResult(false);
            }

            var versionedCache = IotCoreAppDeploymentCache + @"\" + InoVersion;
            var buildOutputDir = ProcessorArchitecture + "\\" + DependencyConfiguration + "\\";
            var fullBuildOutputDir = outputFolder + "\\" + buildOutputDir;

            var linkerArgsBuilder = new StringBuilder();
            linkerArgsBuilder.Append("/OUT:\"" + outputFolder + "\\" + InProcessServerPath + "\" ");
            linkerArgsBuilder.Append("/INCREMENTAL ");
            linkerArgsBuilder.Append("/NOLOGO ");
            linkerArgsBuilder.Append("\"" + versionedCache + @"\lightning\lib\win10-" + ProcessorArchitecture + "\\native\\Lightning.lib\" ");
            linkerArgsBuilder.Append("\"" + SdkRoot + @"Lib\" + SdkVersionString + @"\um\" + ProcessorArchitecture + "\\RUNTIMEOBJECT.LIB\" ");
            linkerArgsBuilder.Append("\"" + SdkRoot + @"Lib\" + SdkVersionString + @"\um\" + ProcessorArchitecture + "\\WINDOWSAPP.LIB\" ");
            linkerArgsBuilder.Append("/MANIFEST:NO ");
            linkerArgsBuilder.Append("/DEBUG:FASTLINK ");
            linkerArgsBuilder.Append("/PDB:\"" + outputFolder + "\\" + PropertiesDisplayName + ".pdb\" ");
            linkerArgsBuilder.Append("/SUBSYSTEM:CONSOLE ");
            linkerArgsBuilder.Append("/TLBID:1 ");
            linkerArgsBuilder.Append("/APPCONTAINER ");
            linkerArgsBuilder.Append("/WINMD ");
            linkerArgsBuilder.Append("/WINMDFILE:\"" + outputFolder + "\\" + PropertiesDisplayName + ".winmd\" ");
            linkerArgsBuilder.Append("/DYNAMICBASE ");
            linkerArgsBuilder.Append("/NXCOMPAT ");
            linkerArgsBuilder.Append("/MACHINE:" + ProcessorArchitecture + " ");
            linkerArgsBuilder.Append("/DLL ");

            // Add LIBPATH entry for VC libs (vccorlib*.lib, etc)
            var libPath = VCLibPath + "\\store";
            if (ProcessorArchitecture == TargetPlatform.ARM)
            {
                libPath += "\\arm";
            }
            linkerArgsBuilder.Append("/LIBPATH:\"" + libPath + "\" ");
            // Add LIBPATH entry for um SDK libs (uuid.lib, etc)
            linkerArgsBuilder.Append("/LIBPATH:\"" + SdkRoot + @"Lib\" + SdkVersionString + @"\um\" + ProcessorArchitecture + "\" ");
            // Add LIBPATH entry for ucrt SDK libs (ucrt*.lib, etc)
            linkerArgsBuilder.Append("/LIBPATH:\"" + SdkRoot + @"Lib\" + SdkVersionString + @"\ucrt\" + ProcessorArchitecture + "\" ");

            foreach (var file in Directory.EnumerateFiles(versionedCache + "\\" + buildOutputDir, "*.obj"))
            {
                linkerArgsBuilder.Append("\"" + file + "\" ");
            }

            foreach (var file in Directory.EnumerateFiles(fullBuildOutputDir, "*.obj"))
            {
                linkerArgsBuilder.Append("\"" + file + "\" ");
            }

            var linkerArgs = linkerArgsBuilder.ToString();
            ExecuteExternalProcess(LinkerPathFromRegistry, VCToolsWorkingDirectoryFromRegistry, linkerArgs, outputFolder + "\\linker.log");

            var dllFilePath = outputFolder + "\\" + InProcessServerPath;
            return Task.FromResult(File.Exists(dllFilePath));
        }

        public Task<bool> BuildAsync(string outputFolder, StreamWriter logging)
        {
            // Find the compiler and linker

            // To build the INO file, we need:
            //   1. Include files
            //   2. Static lib

            // Compile INO file to OBJ
            var compileTask = Compile(outputFolder, logging);
            if (!compileTask.Result)
            {
                logging.WriteLine(Resource.InoProject_CompileStepFailed);
                return Task.FromResult(false);
            }

            // Link OBJ to DLL
            var linkTask = Link(outputFolder, logging);
            if (!linkTask.Result)
            {
                logging.WriteLine(Resource.InoProject_LinkStepFailed);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
