using System;
using System.IO;
using System.Xml;

namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppProjectExtensibility
        {
            public class XmlContentChanges : IContentChange
            {
                public string AppxRelativePath { set; get; }
                public string XPath { set; get; }
                public string Value { set; get; }
                public bool IsAttribute { set; get; }

                public bool ApplyToContent(string rootFolder)
                {
                    var fullPath = rootFolder + @"\" + AppxRelativePath;
                    if (!File.Exists(fullPath))
                    {
                        return false;
                    }

                    var document = new XmlDocument();
                    document.XmlResolver = null;

                    using (var textReader = new XmlTextReader(fullPath))
                    {
                        textReader.DtdProcessing = DtdProcessing.Ignore;
                        document.Load(textReader);
                    }

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
                    return true;
                }
            }
        }
    }
}