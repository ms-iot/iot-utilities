// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;

namespace Microsoft.Iot.IoTCoreSdkProvider
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
