using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Microsoft.Iot.IotCoreTemplateProvider
{
    public class CppBackgroundApplicationTemplate : ITemplate
    {
        public string Name => "C++ Background Application";

        public IBaseProjectTypes GetBaseProjectType()
        {
            return IBaseProjectTypes.CPlusPlusBackgroundApplication;
        }

        private static FileStreamInfo TemplateFileFromResources(string fileName)
        {
            var assemblyName = typeof(CppBackgroundApplicationTemplate).Assembly.GetName().Name;
            var convertedPath = assemblyName + @".Resources.CppBackgroundApplicationTemplate." + fileName.Replace('\\', '.');
            return new FileStreamInfo()
            {
                AppxRelativePath = fileName,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(convertedPath)
            };
        }

        public bool GetAppxMapContents(Collection<string> resourceMetadata, Collection<string> files, string outputFolder)
        {
            resourceMetadata.Add("\"ResourceDimensions\"		\"scale-200\"");
            resourceMetadata.Add("\"ResourceDimensions\"        \"language-en-us\"");
            resourceMetadata.Add("\"ResourceDimensions\"        \"language-en-US\"");
            files.Add("\"" + outputFolder + "\\resources.pri\"         \"resources.pri\"");
            files.Add("\"" + outputFolder + "\\Assets\\Wide310x150Logo.scale-200.png\"            \"Assets\\Wide310x150Logo.scale-200.png\"");
            files.Add("\"" + outputFolder + "\\Assets\\StoreLogo.png\"            \"Assets\\StoreLogo.png\"");
            files.Add("\"" + outputFolder + "\\Assets\\Square44x44Logo.targetsize-24_altform-unplated.png\"           \"Assets\\Square44x44Logo.targetsize-24_altform-unplated.png\"");
            files.Add("\"" + outputFolder + "\\Assets\\Square44x44Logo.scale-200.png\"            \"Assets\\Square44x44Logo.scale-200.png\"");
            files.Add("\"" + outputFolder + "\\Assets\\Square150x150Logo.scale-200.png\"          \"Assets\\Square150x150Logo.scale-200.png\"");
            files.Add("\"" + outputFolder + "\\Assets\\SplashScreen.scale-200.png\"           \"Assets\\SplashScreen.scale-200.png\"");
            files.Add("\"" + outputFolder + "\\Assets\\LockScreenLogo.scale-200.png\"         \"Assets\\LockScreenLogo.scale-200.png\"");
            return true;
        }

        public ReadOnlyCollection<FileStreamInfo> GetTemplateContents()
        {
            var contents = new List<FileStreamInfo>()
                    {
                        TemplateFileFromResources(@"AppxManifest.xml"),
                        TemplateFileFromResources(@"resources.pri"),
                        TemplateFileFromResources(@"TemporaryKey.pfx"),
                        TemplateFileFromResources(@"Assets\LockScreenLogo.scale-200.png"),
                        TemplateFileFromResources(@"Assets\SplashScreen.scale-200.png"),
                        TemplateFileFromResources(@"Assets\Square44x44Logo.scale-200.png"),
                        TemplateFileFromResources(@"Assets\Square44x44Logo.targetsize-24_altform-unplated.png"),
                        TemplateFileFromResources(@"Assets\Square150x150Logo.scale-200.png"),
                        TemplateFileFromResources(@"Assets\StoreLogo.png"),
                        TemplateFileFromResources(@"Assets\Wide310x150Logo.scale-200.png"),
                    };
            return new ReadOnlyCollection<FileStreamInfo>(contents);
        }
    }
}