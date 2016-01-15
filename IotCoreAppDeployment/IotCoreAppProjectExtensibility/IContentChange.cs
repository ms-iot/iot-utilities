using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotCoreAppProjectExtensibility
{
    public interface IContentChange
    {
        void ApplyToContent(String rootFolder);
    }
}
