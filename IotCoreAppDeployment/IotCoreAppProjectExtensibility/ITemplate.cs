using System;
using System.Collections.ObjectModel;

namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppProjectExtensibility
        {
            public interface ITemplate
            {
                string Name { get; }
                IBaseProjectTypes GetBaseProjectType();
                ReadOnlyCollection<FileStreamInfo> GetTemplateContents();
                bool GetAppxMapContents(Collection<string> resourceMetadata, Collection<string> files, string outputFolder);

            }
        }
    }
}