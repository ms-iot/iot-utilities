using System;

namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppProjectExtensibility
        {
            public interface IContentChange
            {
                bool ApplyToContent(string rootFolder);
            }
        }
    }
}