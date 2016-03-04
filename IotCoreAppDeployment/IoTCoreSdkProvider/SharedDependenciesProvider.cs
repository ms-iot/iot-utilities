using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;

namespace Microsoft
{
    namespace Iot
    {
        namespace IoTCoreSdkProvider
        {
            public class SharedDependencyProvider : IDependencyProvider
            {
                public Dictionary<string, IDependency> GetSupportedDependencies()
                {
                    var dependencies = new Dictionary<string, IDependency>();
                    var cppuwp = new CPlusPlusUwpDependency();
                    dependencies.Add(cppuwp.Name, cppuwp);
                    return dependencies;
                }
            }
        }
    }
}