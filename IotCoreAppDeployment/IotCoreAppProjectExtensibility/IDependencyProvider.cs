using System.Collections.Generic;

namespace Microsoft.Iot.IotCoreAppProjectExtensibility
{
    public interface IDependencyProvider
    {
        Dictionary<string, IDependency> GetSupportedDependencies();
    }
}