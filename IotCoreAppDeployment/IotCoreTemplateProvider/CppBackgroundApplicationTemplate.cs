using IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IotCoreTemplateProvider
{
    public class CppBackgroundApplicationTemplate : ITemplate
    {
        public String Name { get { return "C++ Background Application"; } }

        public IBaseProjectTypes GetBaseProjectType()
        {
            return IBaseProjectTypes.CPlusPlusBackgroundApplication;
        }

        FileStreamInfo TemplateFileFromResources(String fileName)
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var convertedPath = @"IotCoreTemplateProvider.Resources.CppBackgroundApplicationTemplate." + fileName.Replace('\\', '.');
            return new FileStreamInfo() {
                AppxRelativePath = fileName,
                Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(convertedPath)
            };
        }

        public void GetAppxMapContents(List<String> resourceMetadata, List<String> files, String outputFolder)
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
        }

        public List<FileStreamInfo> GetTemplateContents()
        {
            var contents = new List<FileStreamInfo>();
            contents.Add(TemplateFileFromResources(@"AppxManifest.xml"));
            contents.Add(TemplateFileFromResources(@"resources.pri"));
            contents.Add(TemplateFileFromResources(@"TemporaryKey.pfx"));
            contents.Add(TemplateFileFromResources(@"TemporaryKey.pfx.cer"));
            contents.Add(TemplateFileFromResources(@"Assets\LockScreenLogo.scale-200.png"));
            contents.Add(TemplateFileFromResources(@"Assets\SplashScreen.scale-200.png"));
            contents.Add(TemplateFileFromResources(@"Assets\Square44x44Logo.scale-200.png"));
            contents.Add(TemplateFileFromResources(@"Assets\Square44x44Logo.targetsize-24_altform-unplated.png"));
            contents.Add(TemplateFileFromResources(@"Assets\Square150x150Logo.scale-200.png"));
            contents.Add(TemplateFileFromResources(@"Assets\StoreLogo.png"));
            contents.Add(TemplateFileFromResources(@"Assets\Wide310x150Logo.scale-200.png"));
            return contents;
        }
    }
}
