namespace IotCoreAppDeployment
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = DeploymentWorker.Execute(args);
            bool result = task.Result;
        }
    }
}
