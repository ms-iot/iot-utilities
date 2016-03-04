using System;
using System.IO;
using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft
{
    namespace Iot
    {
        namespace Python
        {
            public class PythonProject : IProject
            {
                public string Name => "Python Project";
                public string IdentityName => "python-uwp";

                public bool IsSourceSupported(string source)
                {
                    if (source != null)
                    {
                        return source.EndsWith(".py", StringComparison.OrdinalIgnoreCase);
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
                private static string PropertiesDisplayName => "PythonBackgroundApplication1";

                private static string DisplayName => "pythonuwp";
                private static string Description => "pythonuwp";
                private static string ExtensionEntryPoint => "pyuwpbackgroundservice.StartupTask";
                private static string InProcessServerPath => "pyuwpbackgroundservice.dll";
                private static string InProcessServerActivatableClassId => "pyuwpbackgroundservice.StartupTask";

                public ReadOnlyCollection<IContentChange> GetAppxContentChanges()
                {
                    string sdkVersionString = null;
                    switch (SdkVersion)
                    {
                        case SdkVersion.SDK_10_0_10586_0: sdkVersionString = "10.0.10586.0"; break;
                        default:
                            sdkVersionString = "10.0.10240.0"; break; // TODO: throw exception?
                    }
                    var changes = new List<IContentChange>()
                    {
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
                        new AppxManifestCapabilityAddition() {CapabilityName = "internetClientServer"},
                        new AppxManifestCapabilityAddition() { CapabilityName = "privateNetworkClientServer" },
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

                    var assemblyName = typeof(PythonProject).Assembly.GetName().Name;
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
                        FileFromResources(@"Python35.dll"),
                        FileFromResources(@"pyuwpbackgroundservice.dll"),
                        FileFromResources(@"startupinfo.json"),
                        FileFromResources(@"visualstudio_py_debugger.py"),
                        FileFromResources(@"visualstudio_py_launcher.py"),
                        FileFromResources(@"visualstudio_py_remote_launcher.py"),
                        FileFromResources(@"visualstudio_py_repl.py"),
                        FileFromResources(@"visualstudio_py_testlauncher.py"),
                        FileFromResources(@"visualstudio_py_util.py"),
                        FileFromResources(@"PythonHome\DLLs\_bz2.pyd"),
                        FileFromResources(@"PythonHome\DLLs\_ctypes.pyd"),
                        FileFromResources(@"PythonHome\DLLs\_elementtree.pyd"),
                        FileFromResources(@"PythonHome\DLLs\_ptvsdhelper.pyd"),
                        FileFromResources(@"PythonHome\DLLs\_socket.pyd"),
                        FileFromResources(@"PythonHome\DLLs\_ssl.pyd"),
                        FileFromResources(@"PythonHome\DLLs\pyexpat.pyd"),
                        FileFromResources(@"PythonHome\DLLs\select.pyd"),
                        FileFromResources(@"PythonHome\DLLs\unicodedata.pyd"),
                        FileFromResources(@"PythonHome\lib.zip"),
                        FileFromResources(@"ptvsd\attach_server.py"),
                        FileFromResources(@"ptvsd\visualstudio_py_debugger.py"),
                        FileFromResources(@"ptvsd\visualstudio_py_repl.py"),
                        FileFromResources(@"ptvsd\visualstudio_py_util.py"),
                        FileFromResources(@"ptvsd\__init__.py"),
                        FileFromResources(@"ptvsd\__main__.py"),
                        new FileStreamInfo() { AppxRelativePath = "StartupTask.py", Stream = new FileStream(SourceInput, FileMode.Open, FileAccess.Read) },
                    };
                    return new ReadOnlyCollection<FileStreamInfo>(contents);
                }

                public bool GetAppxMapContents(Collection<string> resourceMetadata, Collection<string> files, string outputFolder)
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

                public Task<bool> BuildAsync(string outputFolder, StreamWriter logging)
                {
                    return Task.FromResult(true);
                }
            }
        }
    }
}