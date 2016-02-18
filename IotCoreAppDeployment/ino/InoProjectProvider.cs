using IotCoreAppProjectExtensibility;
using System.Collections.Generic;

namespace Ino
{
    public class InoProjectProvider : IProjectProvider
    {
        public List<IProject> GetSupportedProjects()
        {
            var supportedProjects = new List<IProject>();
            supportedProjects.Add(new InoProject());
            return supportedProjects;
        }
    }
}
