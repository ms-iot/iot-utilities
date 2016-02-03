using System;

namespace IotCoreAppProjectExtensibility
{
    public interface IContentChange
    {
        bool ApplyToContent(String rootFolder);
    }
}
