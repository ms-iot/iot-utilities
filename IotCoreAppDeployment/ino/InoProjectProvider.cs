using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft
{
    namespace Iot
    {
        namespace Ino
        {
            public class InoProjectProvider : IProjectProvider
            {
                public ReadOnlyCollection<IProject> GetSupportedProjects()
                {
                    var supportedProjects = new List<IProject>() {new InoProject()};
                    return new ReadOnlyCollection<IProject>(supportedProjects);
                }
            }
        }
    }
}