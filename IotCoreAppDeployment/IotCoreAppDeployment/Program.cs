namespace IotCoreAppDeployment
{
    class Program
    {
        static void Main(string[] args)
        {
            var stream = System.Console.OpenStandardOutput();
            var task = DeploymentWorker.Execute(args, stream);
            bool result = task.Result;
        }
    }
}
