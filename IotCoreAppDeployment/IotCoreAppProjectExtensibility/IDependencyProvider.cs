using System;
using System.Collections.Generic;

namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppProjectExtensibility
        {
            public interface IDependencyProvider
            {
                Dictionary<string, IDependency> GetSupportedDependencies();
            }
        }
    }
}