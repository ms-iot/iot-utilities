using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
