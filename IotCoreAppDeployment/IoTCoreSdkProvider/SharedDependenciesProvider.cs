using IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;

namespace IoTCoreSdkProvider
{
    public class SharedDependencyProvider : IDependencyProvider
    {
        public Dictionary<String, IDependency> GetSupportedDependencies()
        {
            var dependencies = new Dictionary<String, IDependency>();
            var cppuwp = new CPlusPlusUwpDependency();
            dependencies.Add(cppuwp.Name, cppuwp);
            return dependencies;
        }
    }
}
