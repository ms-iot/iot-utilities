using System.Collections.Generic;

namespace IotCoreAppProjectExtensibility
{
    public interface IProjectProvider
    {
        List<IProject> GetSupportedProjects();
    }
}
