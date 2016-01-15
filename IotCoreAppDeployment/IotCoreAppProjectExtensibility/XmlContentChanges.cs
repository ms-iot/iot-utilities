using System;
using System.Xml;

namespace IotCoreAppProjectExtensibility
{
    public class XmlContentChanges : IContentChange
    {
        public String AppxRelativePath { set; get; }
        public String XPath { set; get; }
        public String Value { set; get; }
        public bool IsAttribute { set; get; }

        public void ApplyToContent(String rootFolder)
        {
            String fullPath = rootFolder + @"\" + AppxRelativePath;
            var document = new XmlDocument();
            document.Load(fullPath);

            var navigator = document.CreateNavigator();
            var xmlnsManager = new System.Xml.XmlNamespaceManager(document.NameTable);
            xmlnsManager.AddNamespace("std", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
            xmlnsManager.AddNamespace("mp", "http://schemas.microsoft.com/appx/2014/phone/manifest");
            xmlnsManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");
            xmlnsManager.AddNamespace("iot", "http://schemas.microsoft.com/appx/manifest/iot/windows10");
            xmlnsManager.AddNamespace("build", "http://schemas.microsoft.com/developer/appx/2015/build");

            var node = navigator.SelectSingleNode(XPath, xmlnsManager);
            node.SetValue(Value);

            document.Save(fullPath);
        }
    }
}
