using System;
using System.Collections.ObjectModel;

namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppProjectExtensibility
        {
            public interface IDependency
            {
                string Name { get; }
                ReadOnlyCollection<FileStreamInfo> GetDependencies(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion);
            }
        }
    }
}