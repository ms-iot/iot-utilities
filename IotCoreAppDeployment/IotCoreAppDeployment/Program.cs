// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Iot.IotCoreAppDeployment
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            using (var stream = System.Console.OpenStandardOutput())
            {
                var result = DeploymentWorker.Execute(args, stream);
                var retval = (result) ? 0 : 1;
                return retval;
            }
        }
    }
}
