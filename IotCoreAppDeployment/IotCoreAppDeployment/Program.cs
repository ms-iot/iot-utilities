namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppDeployment
        {
            public static class Program
            {
                public static int Main(string[] args)
                {
                    using (var stream = System.Console.OpenStandardOutput())
                    {
                        var task = DeploymentWorker.Execute(args, stream);
                        var retval = (task.Result)? 0 : 1;
                        return retval;
                    }
                }
            }
        }
    }
}