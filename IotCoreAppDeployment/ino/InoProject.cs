using System;
using System.IO;
using IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using System.IO.Compression;

namespace Ino
{
    public class InoProject : IProject
    {
        public String Name { get { return "Arduino Wiring Project"; } }
        public String IdentityName { get { return "ino-uwp"; } }

        public bool IsSourceSupported(String source)
        {
            if (source != null)
            {
                return source.EndsWith(".ino", StringComparison.InvariantCultureIgnoreCase);
            }
            return false;
        }

        public IBaseProjectTypes GetBaseProjectType()
        {
            return IBaseProjectTypes.CPlusPlusBackgroundApplication;
        }

        public TargetPlatform ProcessorArchitecture { set; get; }
        public SdkVersion SdkVersion { set; get; }
        private String SdkVersionString
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
        public String SourceInput { set; get; }

        private String IdentityPublisher { get { return "CN=" + PropertiesPublisherDisplayName; } }
        private String PropertiesPublisherDisplayName { get { return "MSFT"; } }

        private String _PhoneIdentityGuid = null;
        private String PhoneIdentityGuid
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
        private String PropertiesDisplayName { get { return "InoBackgroundApplication1"; } }

        private String DisplayName { get { return "inouwp"; } }
        private String Description { get { return "inouwp"; } }
        private String ExtensionEntryPoint { get { return "ArduinoWiringApplication.StartupTask"; } }
        private String InProcessServerPath { get { return PropertiesDisplayName + ".dll"; } }
        private String InProcessServerActivatableClassId { get { return "ArduinoWiringApplication.StartupTask"; } }

        private String _SdkRoot = null;
        private String SdkRoot
        {
            get
            {
                if (_SdkRoot == null)
                {
                    const String universalSdkRootKey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows Kits\Installed Roots";
                    const String universalSdkRootValue = @"KitsRoot10";
                    _SdkRoot = Registry.GetValue(universalSdkRootKey, universalSdkRootValue, null) as String;
                }
                return _SdkRoot;
            }
        }

        private String _RegistryVcCompilerPath = null;
        private String RegistryVcCompilerPath
        {
            get
            {
                if (_RegistryVcCompilerPath == null)
                {
                    _RegistryVcCompilerPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\VisualStudio\VC\19.0\x86\x86", "Compiler", "") as String;
                }
                return _RegistryVcCompilerPath;
            }
        }

        private String _VCToolsWorkingDirectory = null;
        private String VCToolsWorkingDirectoryFromRegistry
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

        private String _CompilerPath = null;
        private String CompilerPathFromRegistry
        {
            get
            {
                if (_CompilerPath == null)
                {
                    String compilerFolder = Path.GetDirectoryName(RegistryVcCompilerPath);
                    _CompilerPath = compilerFolder +
                        ((ProcessorArchitecture == TargetPlatform.ARM) ?
                            @"\x86_arm\cl.exe" :
                            @"\cl.exe");
                }
                return _CompilerPath;
            }
        }

        private String _LinkerPath = null;
        private String LinkerPathFromRegistry
        {
            get
            {
                if (_LinkerPath == null)
                {
                    String compilerFolder = Path.GetDirectoryName(RegistryVcCompilerPath);
                    _LinkerPath = compilerFolder + @"\link.exe";
                }
                return _LinkerPath;
            }
        }

        private String _VCLibPath = null;
        private String VCLibPath
        {
            get
            {
                if (_VCLibPath == null)
                {
                    String vsCommonToolsPath = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
                    if (vsCommonToolsPath != null)
                    {
                        _VCLibPath = new FileInfo(vsCommonToolsPath += "\\..\\..\\VC\\lib").FullName;
                    }
                }
                return _VCLibPath;
            }
        }

        private String _VCIncludePath = null;
        private String VCIncludePath
        {
            get
            {
                if (_VCIncludePath == null)
                {
                    String vsCommonToolsPath = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
                    if (vsCommonToolsPath != null)
                    {
                        _VCIncludePath = new FileInfo(vsCommonToolsPath += "\\..\\..\\VC\\include").FullName;
                    }
                }
                return _VCIncludePath;
            }
        }

        private String _IotCoreAppDeploymentCache = null;
        private String IotCoreAppDeploymentCache
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

        public List<IContentChange> GetAppxContentChanges()
        {
            String sdkVersionString = SdkVersionString;
            var changes = new List<IContentChange>();
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Identity/@Name", Value = IdentityName});
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Identity/@Publisher", Value = IdentityPublisher });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Identity/@ProcessorArchitecture", Value = ProcessorArchitecture.ToString().ToLower() });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/mp:PhoneIdentity/@PhoneProductId", Value = PhoneIdentityGuid });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Properties/std:DisplayName", IsAttribute = false, Value = PropertiesDisplayName });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Properties/std:PublisherDisplayName", IsAttribute = false, Value = PropertiesPublisherDisplayName });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Dependencies/std:TargetDeviceFamily/@MinVersion", Value = sdkVersionString });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Dependencies/std:TargetDeviceFamily/@MaxVersionTested", Value = sdkVersionString });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Applications/std:Application/uap:VisualElements/@DisplayName", Value = DisplayName });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Applications/std:Application/uap:VisualElements/@Description", Value = Description });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Applications/std:Application/std:Extensions/std:Extension/@EntryPoint", Value = ExtensionEntryPoint });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Extensions/std:Extension/std:InProcessServer/std:Path", IsAttribute = false, Value = InProcessServerPath });
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Extensions/std:Extension/std:InProcessServer/std:ActivatableClass/@ActivatableClassId", Value = InProcessServerActivatableClassId });
            return changes;
        }

        public List<IContentChange> GetCapabilities()
        {
            var changes = new List<IContentChange>();

            changes.Add(new AppxManifestCapabilityAddition() { CapabilityName = "internetClientServer" });
            changes.Add(new AppxManifestCapabilityAddition() { CapabilityName = "lowLevelDevices", CapabilityNamespace = "iot" });
            changes.Add(new AppxManifestCapabilityAddition() { Capability = "DeviceCapability", CapabilityName = "109b86ad-f53d-4b76-aa5f-821e2ddf2141" });

            return changes;
        }

        public List<FileStreamInfo> GetAppxContents()
        {
            return new List<FileStreamInfo>();
        }

        public bool GetAppxMapContents(List<String> resourceMetadata, List<String> files, String outputFolder)
        {
            files.Add("\"" + outputFolder + "\\" + PropertiesDisplayName + ".dll\" \"" + PropertiesDisplayName + ".dll\"");
            files.Add("\"" + outputFolder + "\\" + PropertiesDisplayName + ".winmd\" \"" + PropertiesDisplayName + ".winmd\"");
            return true;
        }

        public List<FileStreamInfo> GetDependencies(List<IDependencyProvider> availableDependencyProviders)
        {
            foreach (var dependencyProvider in availableDependencyProviders)
            {
                var supportedDependencies = dependencyProvider.GetSupportedDependencies();
                if (supportedDependencies.ContainsKey("CPlusPlusUwp"))
                {
                    return supportedDependencies["CPlusPlusUwp"].GetDependencies(ProcessorArchitecture, DependencyConfiguration, SdkVersion);
                }
            }
            return new List<FileStreamInfo>();
        }

        private void ExecuteExternalProcess(String executableFileName, String workingDirectory, String arguments, String logFileName)
        {
            Process process = new Process();
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
        private bool CompileFile(String sourceFile, String sourceRoot, String cachedRoot, String fullBuildOutputDir, bool useCachedVersionIfAvailable)
        {
            return CompileFiles(new String[] { sourceFile }, sourceRoot, cachedRoot, fullBuildOutputDir, useCachedVersionIfAvailable);
        }

        private bool CompileFiles(String [] sourceFiles, String sourceRoot, String cachedRoot, String fullBuildOutputDir, bool useCachedVersionIfAvailable)
        {
            var filesToBuild = new StringBuilder();
            foreach (var sourceFile in sourceFiles)
            {
                var sourceFileInfo = new FileInfo(sourceFile);
                String objFilePath = fullBuildOutputDir + "\\" + sourceFileInfo.Name.Replace(sourceFileInfo.Extension, ".obj");
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
            compilerArgsBuilder.Append("/I\"" + sourceRoot + "\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + cachedRoot + "\\arduino\\build\\native\\include\\\\\" ");
            compilerArgsBuilder.Append("/I\"" + cachedRoot + "\\lightning\\build\\native\\include\\\\\" ");
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

            compilerArgsBuilder.Append("/FU\"" + VCLibPath + "\\STORE\\REFERENCES\\PLATFORM.WINMD\" ");

            String winmdReferenceFormat = "/FU\"" + SdkRoot + "REFERENCES\\{0}\\{1}\\{2}\" ";
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATEDEVENTSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATEDEVENTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATIONCAMERASETTINGSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.ACTIVATIONCAMERASETTINGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.CONTACTACTIVATEDEVENTSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.CONTACTACTIVATEDEVENTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.ACTIVATION.WEBUISEARCHACTIVATEDEVENTSCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.ACTIVATION.WEBUISEARCHACTIVATEDEVENTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.BACKGROUND.BACKGROUNDALARMAPPLICATIONCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.BACKGROUND.BACKGROUNDALARMAPPLICATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.CALLS.BACKGROUND.CALLSBACKGROUNDCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.CALLS.BACKGROUND.CALLSBACKGROUNDCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.CALLS.LOCKSCREENCALLCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.CALLS.LOCKSCREENCALLCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.RESOURCES.MANAGEMENT.RESOURCEINDEXERCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.RESOURCES.MANAGEMENT.RESOURCEINDEXERCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.SEARCH.CORE.SEARCHCORECONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.SEARCH.CORE.SEARCHCORECONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.SEARCH.SEARCHCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.SEARCH.SEARCHCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.WALLET.WALLETCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.WALLET.WALLETCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.DEVICES.CUSTOM.CUSTOMDEVICECONTRACT", "1.0.0.0", "WINDOWS.DEVICES.CUSTOM.CUSTOMDEVICECONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.DEVICES.PORTABLE.PORTABLEDEVICECONTRACT", "1.0.0.0", "WINDOWS.DEVICES.PORTABLE.PORTABLEDEVICECONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.DEVICES.PRINTERS.EXTENSIONS.EXTENSIONSCONTRACT", "2.0.0.0", "WINDOWS.DEVICES.PRINTERS.EXTENSIONS.EXTENSIONSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.DEVICES.SCANNERS.SCANNERDEVICECONTRACT", "1.0.0.0", "WINDOWS.DEVICES.SCANNERS.SCANNERDEVICECONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.DEVICES.SMS.LEGACYSMSAPICONTRACT", "1.0.0.0", "WINDOWS.DEVICES.SMS.LEGACYSMSAPICONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.GAMING.PREVIEW.GAMESENUMERATIONCONTRACT", "1.0.0.0", "WINDOWS.GAMING.PREVIEW.GAMESENUMERATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.GLOBALIZATION.GLOBALIZATIONJAPANESEPHONETICANALYZERCONTRACT", "1.0.0.0", "WINDOWS.GLOBALIZATION.GLOBALIZATIONJAPANESEPHONETICANALYZERCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MANAGEMENT.DEPLOYMENT.PREVIEW.DEPLOYMENTPREVIEWCONTRACT", "1.0.0.0", "WINDOWS.MANAGEMENT.DEPLOYMENT.PREVIEW.DEPLOYMENTPREVIEWCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MANAGEMENT.ORCHESTRATION.ORCHESTRATIONCONTRACT", "1.0.0.0", "WINDOWS.MANAGEMENT.ORCHESTRATION.ORCHESTRATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MANAGEMENT.WORKPLACE.WORKPLACESETTINGSCONTRACT", "1.0.0.0", "WINDOWS.MANAGEMENT.WORKPLACE.WORKPLACESETTINGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MEDIA.CAPTURE.APPCAPTURECONTRACT", "2.0.0.0", "WINDOWS.MEDIA.CAPTURE.APPCAPTURECONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MEDIA.CAPTURE.CAMERACAPTUREUICONTRACT", "1.0.0.0", "WINDOWS.MEDIA.CAPTURE.CAMERACAPTUREUICONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MEDIA.DEVICES.CALLCONTROLCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.DEVICES.CALLCONTROLCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MEDIA.MEDIACONTROLCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.MEDIACONTROLCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MEDIA.PLAYLISTS.PLAYLISTSCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.PLAYLISTS.PLAYLISTSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.MEDIA.PROTECTION.PROTECTIONRENEWALCONTRACT", "1.0.0.0", "WINDOWS.MEDIA.PROTECTION.PROTECTIONRENEWALCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.NETWORKING.NETWORKOPERATORS.LEGACYNETWORKOPERATORSCONTRACT", "1.0.0.0", "WINDOWS.NETWORKING.NETWORKOPERATORS.LEGACYNETWORKOPERATORSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.NETWORKING.SOCKETS.CONTROLCHANNELTRIGGERCONTRACT", "1.0.0.0", "WINDOWS.NETWORKING.SOCKETS.CONTROLCHANNELTRIGGERCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SECURITY.ENTERPRISEDATA.ENTERPRISEDATACONTRACT", "2.0.0.0", "WINDOWS.SECURITY.ENTERPRISEDATA.ENTERPRISEDATACONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SECURITY.EXCHANGEACTIVESYNCPROVISIONING.EASCONTRACT", "1.0.0.0", "WINDOWS.SECURITY.EXCHANGEACTIVESYNCPROVISIONING.EASCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SERVICES.MAPS.GUIDANCECONTRACT", "2.0.0.0", "WINDOWS.SERVICES.MAPS.GUIDANCECONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SERVICES.MAPS.LOCALSEARCHCONTRACT", "2.0.0.0", "WINDOWS.SERVICES.MAPS.LOCALSEARCHCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SYSTEM.PROFILE.PROFILEHARDWARETOKENCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.PROFILE.PROFILEHARDWARETOKENCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SYSTEM.PROFILE.PROFILERETAILINFOCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.PROFILE.PROFILERETAILINFOCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SYSTEM.PROFILE.SYSTEMMANUFACTURERS.SYSTEMMANUFACTURERSCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.PROFILE.SYSTEMMANUFACTURERS.SYSTEMMANUFACTURERSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SYSTEM.USERPROFILE.USERPROFILECONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.USERPROFILE.USERPROFILECONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.SYSTEM.USERPROFILE.USERPROFILELOCKSCREENCONTRACT", "1.0.0.0", "WINDOWS.SYSTEM.USERPROFILE.USERPROFILELOCKSCREENCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.UI.APPLICATIONSETTINGS.APPLICATIONSSETTINGSCONTRACT", "1.0.0.0", "WINDOWS.UI.APPLICATIONSETTINGS.APPLICATIONSSETTINGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.UI.CORE.ANIMATIONMETRICS.ANIMATIONMETRICSCONTRACT", "1.0.0.0", "WINDOWS.UI.CORE.ANIMATIONMETRICS.ANIMATIONMETRICSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.UI.CORE.COREWINDOWDIALOGSCONTRACT", "1.0.0.0", "WINDOWS.UI.CORE.COREWINDOWDIALOGSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.UI.XAML.HOSTING.HOSTINGCONTRACT", "1.0.0.0", "WINDOWS.UI.XAML.HOSTING.HOSTINGCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.WEB.HTTP.DIAGNOSTICS.HTTPDIAGNOSTICSCONTRACT", "1.0.0.0", "WINDOWS.WEB.HTTP.DIAGNOSTICS.HTTPDIAGNOSTICSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.APPLICATIONMODEL.CALLS.CALLSVOIPCONTRACT", "1.0.0.0", "WINDOWS.APPLICATIONMODEL.CALLS.CALLSVOIPCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.DEVICES.PRINTERS.PRINTERSCONTRACT", "1.0.0.0", "WINDOWS.DEVICES.PRINTERS.PRINTERSCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.FOUNDATION.FOUNDATIONCONTRACT", "1.0.0.0", "WINDOWS.FOUNDATION.FOUNDATIONCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.FOUNDATION.UNIVERSALAPICONTRACT", "2.0.0.0", "WINDOWS.FOUNDATION.UNIVERSALAPICONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.GRAPHICS.PRINTING3D.PRINTING3DCONTRACT", "2.0.0.0", "WINDOWS.GRAPHICS.PRINTING3D.PRINTING3DCONTRACT.WINMD"));
            compilerArgsBuilder.Append(String.Format(winmdReferenceFormat, "WINDOWS.NETWORKING.CONNECTIVITY.WWANCONTRACT", "1.0.0.0", "WINDOWS.NETWORKING.CONNECTIVITY.WWANCONTRACT.WINMD"));
            compilerArgsBuilder.Append("/analyze- ");

            compilerArgsBuilder.Append(filesToBuild.ToString());

            // Compile the given files
            string compilerArgs = compilerArgsBuilder.ToString();
            ExecuteExternalProcess(CompilerPathFromRegistry, VCToolsWorkingDirectoryFromRegistry, compilerArgs, sourceRoot + "\\compile.log");

            // Check for the resulting obj file to determine success
            foreach (var sourceFile in sourceFiles)
            {
                var sourceFileInfo = new FileInfo(sourceFile);
                var extension = sourceFileInfo.Extension;
                var filename = sourceFileInfo.Name;

                var objFilename = filename.Substring(0, filename.Length - extension.Length) + ".obj";
                String objFilePath = fullBuildOutputDir + "\\" + objFilename;
                if (!File.Exists(objFilePath))
                {
                    Debug.WriteLine(String.Format("Failed to compile {0} to {1}.", sourceFile, objFilePath));
                    return false;
                }
            }
            return true;
        }

        private bool CopyResourceFileBuildFolderAndUnzip(String resourceName, String outputFolder, String unzipFolder)
        {
            var status = CopyResourceFileBuildFolder(resourceName, outputFolder);
            if (!status)
            {
                return false;
            }

            ZipFile.ExtractToDirectory(outputFolder + @"\" + resourceName, outputFolder + "\\" + unzipFolder);
            return Directory.Exists(outputFolder + "\\" + unzipFolder);
        }
        private bool CopyResourceFileBuildFolder(String resourceName, String outputFolder)
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var resource = String.Format(@"Ino.Resources.{0}", resourceName);
            new FileStreamInfo()
            {
                AppxRelativePath = resourceName,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            }.Apply(outputFolder);

            return File.Exists(outputFolder + "\\" + resourceName);
        }

        private async Task<bool> Compile(String outputFolder, StreamWriter logging)
        {
            if (!File.Exists(CompilerPathFromRegistry))
            {
                logging.WriteLine(String.Format("Compiler cannot be found. {0}", CompilerPathFromRegistry));
                return false;
            }

            if (SdkRoot == null)
            {
                logging.WriteLine(String.Format("SDK cannot be found."));
                return false;
            }

            if (VCIncludePath == null)
            {
                logging.WriteLine(String.Format("VC Include files cannot be found."));
                return false;
            }

            if (VCLibPath == null)
            {
                logging.WriteLine(String.Format("VC library files cannot be found. (platform.winmd)"));
                return false;
            }

            // Create build folder (i.e. ARM\Debug or X86\Debug)
            String buildOutputDir = ProcessorArchitecture + "\\" + DependencyConfiguration + "\\";
            String fullBuildOutputDir = outputFolder + "\\" + buildOutputDir;
            if (!Directory.Exists(fullBuildOutputDir))
            {
                Directory.CreateDirectory(fullBuildOutputDir);
            }
            bool success = false;

            // Get PCH.H from resources
            success = CopyResourceFileBuildFolder("pch.h", outputFolder);
            if (!success)
            {
                Debug.WriteLine(String.Format("Failed to copy pch.h from resources"));
                return false;
            }
            // Get StartupTask.cpp from resources
            success = CopyResourceFileBuildFolder("StartupTask.cpp", outputFolder);
            if (!success)
            {
                Debug.WriteLine(String.Format("Failed to copy StartupTask.cpp from resources"));
                return false;
            }

            String versionedCache = IotCoreAppDeploymentCache + "\\" + Assembly.GetAssembly(typeof(InoProject)).GetName().Version;
            String versionedConfigCache = versionedCache + "\\" + buildOutputDir;
            if (!Directory.Exists(versionedCache))
            {
                Directory.CreateDirectory(versionedCache);
            }
            if (!Directory.Exists(versionedConfigCache))
            {
                Directory.CreateDirectory(versionedConfigCache);
            }
            if (!Directory.Exists(versionedCache + "\\arduino"))
            {
                // Get arduino sources from resources
                success = CopyResourceFileBuildFolderAndUnzip("microsoft.iot.sdkfromarduino.1.1.1.nupkg", versionedCache, "arduino");
                if (!success)
                {
                    Debug.WriteLine(String.Format("Failed to copy microsoft.iot.sdkfromarduino.1.1.1.nupkg from resources and unzip it"));
                    return false;
                }
            }
            if (!Directory.Exists(versionedCache + "\\lightning"))
            {
                // Get lightning sources from resources
                success = CopyResourceFileBuildFolderAndUnzip("microsoft.iot.lightning.1.0.2-alpha.nupkg", versionedCache, "lightning");
                if (!success)
                {
                    Debug.WriteLine(String.Format("Failed to copy microsoft.iot.lightning.1.0.2-alpha.nupkg from resources and unzip it"));
                    return false;
                }
            }

            String[] dependencySourceCodeToBuild = new String[] {
                versionedCache + "\\arduino\\build\\native\\source\\IPAddress.cpp",
                versionedCache + "\\arduino\\build\\native\\source\\LiquidCrystal.cpp",
                versionedCache + "\\arduino\\build\\native\\source\\Print.cpp",
                versionedCache + "\\arduino\\build\\native\\source\\Stepper.cpp",
                versionedCache + "\\arduino\\build\\native\\source\\Stream.cpp",
                versionedCache + "\\arduino\\build\\native\\source\\WString.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\arduino.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\BoardPins.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\BcmI2cController.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\BcmSpiController.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\BtI2cController.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\BtSpiController.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\CY8C9540ASupport.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\DmapSupport.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\DmapErrors.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\eeprom.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\GpioController.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\HardwareSerial.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\I2c.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\I2cController.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\I2cTransaction.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\NetworkSerial.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\PCA9685Support.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\PCAL9535ASupport.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\PulseIn.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\Servo.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\Spi.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\SpiController.cpp",
                versionedCache + "\\lightning\\build\\native\\source\\QuarkSpiController.cpp",
            };

            // Compile the arduino and lightning sources
            success = CompileFiles(dependencySourceCodeToBuild, outputFolder, versionedCache, versionedCache + "\\" + buildOutputDir, true);
            if (!success)
            {
                logging.WriteLine("... failed to compile dependency source code.");
                return false;
            }

            // Compile StartupTask.cpp
            success = CompileFile(outputFolder + @"\StartupTask.cpp", outputFolder, versionedCache, fullBuildOutputDir, false);
            if (!success)
            {
                logging.WriteLine("... failed to compile dependency source code.");
                Debug.WriteLine(String.Format("Failed to compile {0}.", outputFolder + @"\StartupTask.cpp"));
                return false;
            }
            // Compile the users INO input file
            success = CompileFile(SourceInput, outputFolder, versionedCache, fullBuildOutputDir, false);
            if (!success)
            {
                logging.WriteLine(String.Format("... failed to compile {0}.", SourceInput));
                return false;
            }

            return success;
        }

        private async Task<bool> Link(String outputFolder, StreamWriter logging)
        {
            if (!File.Exists(LinkerPathFromRegistry))
            {
                logging.WriteLine(String.Format("Linker cannot be found. {0}", LinkerPathFromRegistry));
                return false;
            }

            if (SdkRoot == null)
            {
                logging.WriteLine(String.Format("SDK cannot be found."));
                return false;
            }

            if (VCLibPath == null)
            {
                logging.WriteLine(String.Format("VC library files cannot be found."));
                return false;
            }

            String versionedCache = IotCoreAppDeploymentCache + "\\" + Assembly.GetAssembly(typeof(InoProject)).GetName().Version;
            String buildOutputDir = ProcessorArchitecture + "\\" + DependencyConfiguration + "\\";
            String fullBuildOutputDir = outputFolder + "\\" + buildOutputDir;

            var linkerArgsBuilder = new StringBuilder();
            linkerArgsBuilder.Append("/OUT:\"" + outputFolder + "\\" + InProcessServerPath + "\" ");
            linkerArgsBuilder.Append("/INCREMENTAL ");
            linkerArgsBuilder.Append("/NOLOGO ");
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
            String libPath = VCLibPath + "\\store";
            if (ProcessorArchitecture == TargetPlatform.ARM)
            {
                libPath += "\\arm";
            }
            linkerArgsBuilder.Append("/LIBPATH:\"" + libPath +"\" ");
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

            string linkerArgs = linkerArgsBuilder.ToString();
            ExecuteExternalProcess(LinkerPathFromRegistry, VCToolsWorkingDirectoryFromRegistry, linkerArgs, outputFolder + "\\linker.log");

            String dllFilePath = outputFolder + "\\" + InProcessServerPath;
            bool success = File.Exists(dllFilePath);
            return success;
        }

        public async Task<bool> BuildAsync(String outputFolder, StreamWriter logging)
        {
            bool success = false;

            // Find the compiler and linker

            // To build the INO file, we need:
            //   1. Include files
            //   2. Static lib

            // Compile INO file to OBJ
            success = await Compile(outputFolder, logging);
            if (!success)
            {
                logging.WriteLine("...compile step failed!");
                return false;
            }

            // Link OBJ to DLL
            success = await Link(outputFolder, logging);
            if (!success)
            {
                logging.WriteLine("... linking step failed!");
                return false;
            }

            return success;
        }
    }
}
