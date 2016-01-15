using IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
