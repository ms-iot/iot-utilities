using System.Collections.Generic;

namespace Python
{
    public class PythonProjectProvider : IotCoreAppProjectExtensibility.IProjectProvider
    {
        public List<IotCoreAppProjectExtensibility.IProject> GetSupportedProjects()
        {
            var supportedProjects = new List<IotCoreAppProjectExtensibility.IProject>();
            supportedProjects.Add(new PythonProject());
            return supportedProjects;
        }
    }
}
