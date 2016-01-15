using System;
using System.Collections.Generic;

namespace IotCoreAppProjectExtensibility
{
    public interface IDependencyProvider
    {
        Dictionary<String, IDependency> GetSupportedDependencies();
    }
}
