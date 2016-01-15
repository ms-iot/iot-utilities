using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotCoreAppProjectExtensibility
{
    public interface IDependency
    {
        String Name { get; }
        List<FileStreamInfo> GetDependencies(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion);
    }
}
