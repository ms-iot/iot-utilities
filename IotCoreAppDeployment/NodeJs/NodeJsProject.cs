using System;
using System.IO;
using IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace NodeJs
{
    public class NodeJsProject : IProject
    {
        public String Name { get { return "Node.js Project"; } }
        public String IdentityName { get { return "nodejs-uwp"; } }

        public bool IsSourceSupported(String source)
        {
            if (source != null)
            {
                return source.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase);
            }
            return false;
        }

        public IBaseProjectTypes GetBaseProjectType()
        {
            return IBaseProjectTypes.CPlusPlusBackgroundApplication;
        }

        public TargetPlatform ProcessorArchitecture { set; get; }
        public SdkVersion SdkVersion { set; get; }
        public DependencyConfiguration DependencyConfiguration { set; get; }
        public String SourceInput { set; get; }

        private String IdentityPublisher { get { return "CN=" + "MSFT" /*Environment.UserName*/; } }
        private String PropertiesPublisherDisplayName { get { return "MSFT" /*Environment.UserName*/; } }

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
        private String PropertiesDisplayName { get { return "NodejsWebServer1"; } }

        private String DisplayName { get { return "nodeuwp"; } }
        private String Description { get { return "nodeuwp"; } }
        private String ExtensionEntryPoint { get { return "nodeuwp.StartupTask"; } }
        private String InProcessServerPath { get { return "nodeuwp.dll"; } }
        private String InProcessServerActivatableClassId { get { return "nodeuwp.StartupTask"; } }

        public List<IContentChange> GetAppxContentChanges()
        {
            String sdkVersionString = null;
            switch (SdkVersion)
            {
                case SdkVersion.SDK_10_0_10586_0: sdkVersionString = "10.0.10586.0"; break;
                default:
                    sdkVersionString = "10.0.10240.0"; break; // TODO: throw exception?
            }
            var changes = new List<IContentChange>();
            changes.Add(new XmlContentChanges() { AppxRelativePath = @"AppxManifest.xml", XPath = @"/std:Package/std:Identity/@Name", Value = IdentityName });
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
            changes.Add(new AppxManifestCapabilityAddition() { CapabilityName = "privateNetworkClientServer" });
            changes.Add(new AppxManifestCapabilityAddition() { CapabilityName = "systemManagement", CapabilityNamespace = "iot" });
            changes.Add(new AppxManifestCapabilityAddition() { Capability = "DeviceCapability", CapabilityName = "serialcommunication", DeviceId = "any", FunctionType="name:serialPort" });

            return changes;
        }

        FileStreamInfo FileFromResources(String fileName)
        {
            var platformString = "";
            switch (ProcessorArchitecture)
            {
                case TargetPlatform.X86: platformString = "x86"; break;
                case TargetPlatform.ARM: platformString = "ARM"; break;
                default:
                    return null;
            }

            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var convertedPath = @"NodeJs.Resources." + platformString + "." + fileName.Replace('\\', '.');
            return new FileStreamInfo()
            {
                AppxRelativePath = fileName,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(convertedPath)
            };
        }

        public List<FileStreamInfo> GetAppxContents()
        {
            var contents = new List<FileStreamInfo>();
            contents.Add(FileFromResources(@"msvcp140.dll"));
            contents.Add(FileFromResources(@"node.dll"));
            contents.Add(FileFromResources(@"nodeuwp.dll"));
            contents.Add(FileFromResources(@"startupinfo.xml"));
            contents.Add(FileFromResources(@"vccorlib140.dll"));
            contents.Add(FileFromResources(@"vcruntime140.dll"));
            contents.Add(FileFromResources(@"node_modules\uwp.node"));

            contents.Add(new FileStreamInfo() { AppxRelativePath = "server.js", Stream = new FileStream(SourceInput, FileMode.Open, FileAccess.Read) });
            return contents;
        }

        public void GetAppxMapContents(List<String> resourceMetadata, List<String> files, String outputFolder)
        {
            files.Add("\"" + outputFolder + "\\node_modules\\uwp.node\"         \"node_modules\\uwp.node\"");
            files.Add("\"" + outputFolder + "\\msvcp140.dll\"         \"msvcp140.dll\"");
            files.Add("\"" + outputFolder + "\\node.dll\"         \"node.dll\"");
            files.Add("\"" + outputFolder + "\\nodeuwp.dll\"         \"nodeuwp.dll\"");
            files.Add("\"" + outputFolder + "\\server.js\"         \"server.js\"");
            files.Add("\"" + outputFolder + "\\startupinfo.xml\"         \"startupinfo.xml\"");
            files.Add("\"" + outputFolder + "\\vccorlib140.dll\"         \"vccorlib140.dll\"");
            files.Add("\"" + outputFolder + "\\vcruntime140.dll\"         \"vcruntime140.dll\"");
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

        public async Task<bool> BuildAsync(String outputFolder, StreamWriter logging)
        {
            return true;
        }
    }
}
