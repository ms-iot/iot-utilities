using System.Collections.Generic;

namespace NodeJs
{
    public class NodeJsProjectProvider : IotCoreAppProjectExtensibility.IProjectProvider
    {
        public List<IotCoreAppProjectExtensibility.IProject> GetSupportedProjects()
        {
            var supportedProjects = new List<IotCoreAppProjectExtensibility.IProject>();
            supportedProjects.Add(new NodeJsProject());
            return supportedProjects;
        }
    }
}
