using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotCoreAppProjectExtensibility
{
    public enum TargetPlatform
    {
        ARM,
        X86,
        Unknown,
    }
    public enum SdkVersion
    {
        SDK_10_10586_0,
        Unknown,
    }

    public enum DependencyConfiguration
    {
        Debug,
        Release,
        Unknown
    }

}
