using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Iot.NodeJs
{
    public class NodeJsProjectProvider : IProjectProvider
    {
        public ReadOnlyCollection<IProject> GetSupportedProjects()
        {
            var supportedProjects = new List<IProject>() { new NodeJsProject() };
            return new ReadOnlyCollection<IProject>(supportedProjects);
        }
    }
}