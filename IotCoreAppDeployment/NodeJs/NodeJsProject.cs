// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Iot.NodeJs
{
    public class NodeJsProject : IProject
    {
        public string Name => "Node.js Project";
        public string IdentityName => "nodejs-uwp";

        public bool IsSourceSupported(string source)
        {
            if (source != null)
            {
                return source.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
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
        public string SourceInput { set; get; }

        private static string IdentityPublisher => "CN=" + PropertiesPublisherDisplayName;
        private static string PropertiesPublisherDisplayName => "MSFT";

        private static string _PhoneIdentityGuid = null;
        private static string PhoneIdentityGuid
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

        private static string PropertiesDisplayName => "NodejsWebServer1";

        private static string DisplayName => "nodeuwp";
        private static string Description => "nodeuwp";
        private static string ExtensionEntryPoint => "nodeuwp.StartupTask";
        private static string InProcessServerPath => "nodeuwp.dll";
        private static string InProcessServerActivatableClassId => "nodeuwp.StartupTask";

        public ReadOnlyCollection<IContentChange> GetAppxContentChanges()
        {
            string sdkVersionString = null;
            switch (SdkVersion)
            {
                case SdkVersion.SDK_10_0_10586_0: sdkVersionString = "10.0.10586.0"; break;
                default:
                    sdkVersionString = "10.0.10240.0"; break; // TODO: throw exception?
            }
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
            var changes = new List<IContentChange>()
                    {
                        new AppxManifestCapabilityAddition() { CapabilityName = "internetClientServer" },
                        new AppxManifestCapabilityAddition() { CapabilityName = "privateNetworkClientServer" },
                        new AppxManifestCapabilityAddition() { CapabilityName = "systemManagement", CapabilityNamespace = "iot" },
                        new AppxManifestCapabilityAddition() { Capability = "DeviceCapability", CapabilityName = "serialcommunication", DeviceId = "any", FunctionType = "name:serialPort" },
                    };

            return new ReadOnlyCollection<IContentChange>(changes);
        }

        private FileStreamInfo FileFromResources(string fileName)
        {
            var platformString = "";
            switch (ProcessorArchitecture)
            {
                case TargetPlatform.X86: platformString = "x86"; break;
                case TargetPlatform.ARM: platformString = "ARM"; break;
                default:
                    return null;
            }

            string assemblyName = typeof(NodeJsProject).Assembly.GetName().Name;
            var convertedPath = assemblyName + @".Resources." + platformString + "." + fileName.Replace('\\', '.');
            return new FileStreamInfo()
            {
                AppxRelativePath = fileName,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(convertedPath)
            };
        }

        public ReadOnlyCollection<FileStreamInfo> GetAppxContents()
        {
            var contents = new List<FileStreamInfo>() {
                        FileFromResources(@"msvcp140.dll"),
                        FileFromResources(@"node.dll"),
                        FileFromResources(@"nodeuwp.dll"),
                        FileFromResources(@"startupinfo.xml"),
                        FileFromResources(@"vccorlib140.dll"),
                        FileFromResources(@"vcruntime140.dll"),
                        FileFromResources(@"node_modules\uwp.node"),
                        new FileStreamInfo() { AppxRelativePath = "server.js", Stream = new FileStream(SourceInput, FileMode.Open, FileAccess.Read) },
                    };
            return new ReadOnlyCollection<FileStreamInfo>(contents);
        }

        public bool GetAppxMapContents(Collection<string> resourceMetadata, Collection<string> files, string outputFolder)
        {
            files.Add("\"" + outputFolder + "\\node_modules\\uwp.node\"         \"node_modules\\uwp.node\"");
            files.Add("\"" + outputFolder + "\\msvcp140.dll\"         \"msvcp140.dll\"");
            files.Add("\"" + outputFolder + "\\node.dll\"         \"node.dll\"");
            files.Add("\"" + outputFolder + "\\nodeuwp.dll\"         \"nodeuwp.dll\"");
            files.Add("\"" + outputFolder + "\\server.js\"         \"server.js\"");
            files.Add("\"" + outputFolder + "\\startupinfo.xml\"         \"startupinfo.xml\"");
            files.Add("\"" + outputFolder + "\\vccorlib140.dll\"         \"vccorlib140.dll\"");
            files.Add("\"" + outputFolder + "\\vcruntime140.dll\"         \"vcruntime140.dll\"");
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
    }
}
