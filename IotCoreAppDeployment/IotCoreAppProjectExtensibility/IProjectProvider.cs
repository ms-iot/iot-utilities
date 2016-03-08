using System.Collections.ObjectModel;

namespace Microsoft.Iot.IotCoreAppProjectExtensibility
{
    public interface IProjectProvider
    {
        ReadOnlyCollection<IProject> GetSupportedProjects();
    }
}