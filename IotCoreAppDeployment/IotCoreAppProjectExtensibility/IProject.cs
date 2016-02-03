using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IotCoreAppProjectExtensibility
{
    public interface IProject
    {
        String Name { get; }
        String IdentityName { get; }

        String SourceInput { set; get; }


        TargetPlatform ProcessorArchitecture { set; get; }
        SdkVersion SdkVersion { set; get; }
        DependencyConfiguration DependencyConfiguration { set; get; }

        bool IsSourceSupported(String source);
        IBaseProjectTypes GetBaseProjectType();

        List<IContentChange> GetCapabilities();
        List<IContentChange> GetAppxContentChanges();
        bool GetAppxMapContents(List<String> resourceMetadata, List<String> files, String outputFolder);
        List<FileStreamInfo> GetAppxContents();
        List<FileStreamInfo> GetDependencies(List<IDependencyProvider> availableDependencyProviders);
        Task<bool> BuildAsync(String outputFolder, StreamWriter logging);
    }
}
