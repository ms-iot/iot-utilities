using System.ComponentModel;

namespace Microsoft
{
    namespace Iot
    {
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
                [Description("10.0.10586.0")]
                SDK_10_0_10586_0,
                [Description("Unknown")]
                Unknown,
            }

            public enum DependencyConfiguration
            {
                Debug,
                Release,
                Unknown
            }

        }
    }
}