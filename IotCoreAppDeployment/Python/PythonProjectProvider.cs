using IotCoreAppProjectExtensibility;
using System.Collections.Generic;

namespace Python
{
    public class PythonProjectProvider : IProjectProvider
    {
        public List<IProject> GetSupportedProjects()
        {
            var supportedProjects = new List<IProject>();
            supportedProjects.Add(new PythonProject());
            return supportedProjects;
        }
    }
}
