using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Iot.Python
{
    public class PythonProjectProvider : IProjectProvider
    {
        public ReadOnlyCollection<IProject> GetSupportedProjects()
        {
            var supportedProjects = new List<IProject>() { new PythonProject() };
            return new ReadOnlyCollection<IProject>(supportedProjects);
        }
    }
}