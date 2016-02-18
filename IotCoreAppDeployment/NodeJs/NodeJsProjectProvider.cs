using IotCoreAppProjectExtensibility;
using System.Collections.Generic;

namespace NodeJs
{
    public class NodeJsProjectProvider : IProjectProvider
    {
        public List<IProject> GetSupportedProjects()
        {
            var supportedProjects = new List<IProject>();
            supportedProjects.Add(new NodeJsProject());
            return supportedProjects;
        }
    }
}
