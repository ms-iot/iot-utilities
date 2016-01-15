using System;
using System.Collections.Generic;
using System.IO;

namespace IotCoreAppProjectExtensibility
{
    public interface IProject
    {
        String Name { get; }

        String SourceInput { set; get; }

        TargetPlatform ProcessorArchitecture { set; get; }
        SdkVersion SdkVersion { set; get; }
        DependencyConfiguration DependencyConfiguration { set; get; }

        bool IsSourceSupported(String source);
        IBaseProjectTypes GetBaseProjectType();

        List<IContentChange> GetAppxContentChanges();
        void GetAppxMapContents(List<String> resourceMetadata, List<String> files, String outputFolder);
        List<FileStreamInfo> GetAppxContents();
        List<FileStreamInfo> GetDependencies(List<IDependencyProvider> availableDependencyProviders);
    }
}
