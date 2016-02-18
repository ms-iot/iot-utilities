using IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace IoTCoreSdkProvider
{
    public class CPlusPlusUwpDependency : IDependency
    {
        public string Name
        {
            get
            {
                return "CPlusPlusUwp";
            }
        }

        FileStreamInfo VCLibsFromResources(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion)
        {
            var vclibVersion = "";
            switch (sdkVersion)
            {
                case SdkVersion.SDK_10_0_10586_0: vclibVersion = "14.00"; break;
                default:
                    return null;
            }

            var platformString = "";
            switch (platform)
            {
                case TargetPlatform.X86: platformString = "x86"; break;
                case TargetPlatform.ARM: platformString = "ARM"; break;
                default:
                    return null;
            }

            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var appxFilename = String.Format("Microsoft.VCLibs.{0}.{1}.{2}.appx", platformString, configuration.ToString(), vclibVersion);
            var convertedPath = @"IoTCoreSdkProvider.Resources.VCLibs." + platformString + "." + appxFilename; 
            return new FileStreamInfo()
            {
                AppxRelativePath = appxFilename,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(convertedPath)
            };
        }

        public List<FileStreamInfo> GetDependencies(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion)
        {
            var dependencies = new List<FileStreamInfo>();
            var dependency = VCLibsFromResources(platform, configuration, sdkVersion);
            if (null != dependency)
            {
                dependencies.Add(dependency);
            }
            return dependencies;
        }
    }
}
