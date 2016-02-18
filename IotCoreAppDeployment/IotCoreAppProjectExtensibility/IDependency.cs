using System;
using System.Collections.Generic;

namespace IotCoreAppProjectExtensibility
{
    public interface IDependency
    {
        String Name { get; }
        List<FileStreamInfo> GetDependencies(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion);
    }
}
