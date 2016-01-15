using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotCoreAppProjectExtensibility
{
    public interface ITemplate
    {
        String Name { get; }
        IBaseProjectTypes GetBaseProjectType();
        List<FileStreamInfo> GetTemplateContents();
        void GetAppxMapContents(List<String> resourceMetadata, List<String> files, String outputFolder);

    }
}
