using System;

namespace IotCoreAppProjectExtensibility
{
    public interface IContentChange
    {
        void ApplyToContent(String rootFolder);
    }
}
