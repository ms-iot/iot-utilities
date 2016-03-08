using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Iot.IoTCoreSdkProvider
{
    public class CPlusPlusUwpDependency : IDependency
    {
        public string Name => "CPlusPlusUwp";

        private static FileStreamInfo VCLibsFromResources(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion)
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

            var appxFilename = string.Format(CultureInfo.InvariantCulture, "Microsoft.VCLibs.{0}.{1}.{2}.appx", platformString, configuration.ToString(), vclibVersion);
            var assemblyName = typeof(CPlusPlusUwpDependency).Assembly.GetName().Name;
            var convertedPath = assemblyName + @".Resources.VCLibs." + platformString + "." + appxFilename;
            return new FileStreamInfo()
            {
                AppxRelativePath = appxFilename,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(convertedPath)
            };
        }

        public ReadOnlyCollection<FileStreamInfo> GetDependencies(TargetPlatform platform, DependencyConfiguration configuration, SdkVersion sdkVersion)
        {
            var dependencies = new List<FileStreamInfo>();
            var dependency = VCLibsFromResources(platform, configuration, sdkVersion);
            if (null != dependency)
            {
                dependencies.Add(dependency);
            }
            return new ReadOnlyCollection<FileStreamInfo>(dependencies);
        }
    }
}