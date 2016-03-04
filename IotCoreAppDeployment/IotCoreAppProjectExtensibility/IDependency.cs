using System.Collections.ObjectModel;

namespace Microsoft.Iot.IotCoreAppProjectExtensibility
{
    public interface IDependency
    {
        string Name { get; }
        ReadOnlyCollection<FileStreamInfo> GetDependencies(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion);
    }
}