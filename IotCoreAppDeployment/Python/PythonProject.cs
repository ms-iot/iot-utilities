using System;
using System.IO;
using IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Reflection;

namespace Python
{
    public class PythonProject : IProject
    {
        public String Name { get { return "Python Project"; } }
        public String IdentityName { get { return "python-uwp"; } }

        public bool IsSourceSupported(String source)
        {
            if (source != null)
            {
                return source.EndsWith(".py", StringComparison.InvariantCultureIgnoreCase);
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
        private String PropertiesDisplayName { get { return "PythonBackgroundApplication1"; } }

        private String DisplayName { get { return "pythonuwp"; } }
        private String Description { get { return "pythonuwp"; } }
        private String ExtensionEntryPoint { get { return "pyuwpbackgroundservice.StartupTask"; } }
        private String InProcessServerPath { get { return "pyuwpbackgroundservice.dll"; } }
        private String InProcessServerActivatableClassId { get { return "pyuwpbackgroundservice.StartupTask"; } }

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
            changes.Add(new AppxManifestCapabilityAddition() { CapabilityName = "privateNetworkClientServer" });

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
            var convertedPath = @"Python.Resources." + platformString + "." + fileName.Replace('\\', '.');
            return new FileStreamInfo()
            {
                AppxRelativePath = fileName,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(convertedPath)
            };
        }

        public List<FileStreamInfo> GetAppxContents()
        {
            var contents = new List<FileStreamInfo>();
            contents.Add(FileFromResources(@"Python35.dll"));
            contents.Add(FileFromResources(@"pyuwpbackgroundservice.dll"));
            contents.Add(FileFromResources(@"startupinfo.json"));
            contents.Add(FileFromResources(@"visualstudio_py_debugger.py"));
            contents.Add(FileFromResources(@"visualstudio_py_launcher.py"));
            contents.Add(FileFromResources(@"visualstudio_py_remote_launcher.py"));
            contents.Add(FileFromResources(@"visualstudio_py_repl.py"));
            contents.Add(FileFromResources(@"visualstudio_py_testlauncher.py"));
            contents.Add(FileFromResources(@"visualstudio_py_util.py"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\_bz2.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\_ctypes.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\_elementtree.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\_ptvsdhelper.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\_socket.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\_ssl.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\pyexpat.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\select.pyd"));
            contents.Add(FileFromResources(@"PythonHome\DLLs\unicodedata.pyd"));
            contents.Add(FileFromResources(@"PythonHome\lib.zip"));
            contents.Add(FileFromResources(@"ptvsd\attach_server.py"));
            contents.Add(FileFromResources(@"ptvsd\visualstudio_py_debugger.py"));
            contents.Add(FileFromResources(@"ptvsd\visualstudio_py_repl.py"));
            contents.Add(FileFromResources(@"ptvsd\visualstudio_py_util.py"));
            contents.Add(FileFromResources(@"ptvsd\__init__.py"));
            contents.Add(FileFromResources(@"ptvsd\__main__.py"));

            contents.Add(new FileStreamInfo() { AppxRelativePath = "StartupTask.py", Stream = new FileStream(SourceInput, FileMode.Open, FileAccess.Read) });
            return contents;
        }

        public void GetAppxMapContents(List<String> resourceMetadata, List<String> files, String outputFolder)
        {
            files.Add("\"" + outputFolder + "\\StartupTask.py\" \"StartupTask.py\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\pyexpat.pyd\" \"PythonHome\\DLLs\\pyexpat.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\select.pyd\" \"PythonHome\\DLLs\\select.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\unicodedata.pyd\" \"PythonHome\\DLLs\\unicodedata.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\_bz2.pyd\" \"PythonHome\\DLLs\\_bz2.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\_ctypes.pyd\" \"PythonHome\\DLLs\\_ctypes.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\_elementtree.pyd\" \"PythonHome\\DLLs\\_elementtree.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\_ptvsdhelper.pyd\" \"PythonHome\\DLLs\\_ptvsdhelper.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\_socket.pyd\" \"PythonHome\\DLLs\\_socket.pyd\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\DLLs\\_ssl.pyd\" \"PythonHome\\DLLs\\_ssl.pyd\"");
            files.Add("\"" + outputFolder + "\\Python35.dll\" \"Python35.dll\"");
            files.Add("\"" + outputFolder + "\\pyuwpbackgroundservice.dll\" \"pyuwpbackgroundservice.dll\"");
            files.Add("\"" + outputFolder + "\\PythonHome\\lib.zip\" \"PythonHome\\lib.zip\"");
            files.Add("\"" + outputFolder + "\\startupinfo.json\" \"startupinfo.json\"");
            files.Add("\"" + outputFolder + "\\visualstudio_py_debugger.py\" \"visualstudio_py_debugger.py\"");
            files.Add("\"" + outputFolder + "\\visualstudio_py_launcher.py\" \"visualstudio_py_launcher.py\"");
            files.Add("\"" + outputFolder + "\\visualstudio_py_repl.py\" \"visualstudio_py_repl.py\"");
            files.Add("\"" + outputFolder + "\\visualstudio_py_testlauncher.py\" \"visualstudio_py_testlauncher.py\"");
            files.Add("\"" + outputFolder + "\\visualstudio_py_util.py\" \"visualstudio_py_util.py\"");
            files.Add("\"" + outputFolder + "\\visualstudio_py_remote_launcher.py\" \"visualstudio_py_remote_launcher.py\"");

            files.Add("\"" + outputFolder + "\\ptvsd\\attach_server.py\" \"ptvsd\\attach_server.py\"");
            files.Add("\"" + outputFolder + "\\ptvsd\\visualstudio_py_debugger.py\" \"ptvsd\\visualstudio_py_debugger.py\"");
            files.Add("\"" + outputFolder + "\\ptvsd\\visualstudio_py_repl.py\" \"ptvsd\\visualstudio_py_repl.py\"");
            files.Add("\"" + outputFolder + "\\ptvsd\\visualstudio_py_util.py\" \"ptvsd\\visualstudio_py_util.py\"");
            files.Add("\"" + outputFolder + "\\ptvsd\\__init__.py\" \"ptvsd\\__init__.py\"");
            files.Add("\"" + outputFolder + "\\ptvsd\\__main__.py\" \"ptvsd\\__main__.py\"");
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

    }
}
