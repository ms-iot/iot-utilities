using System.Collections.ObjectModel;

namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppProjectExtensibility
        {
            public interface IProjectProvider
            {
                ReadOnlyCollection<IProject> GetSupportedProjects();
            }
        }
    }
}