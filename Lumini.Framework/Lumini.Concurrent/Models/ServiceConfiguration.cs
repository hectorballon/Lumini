using System;
using Lumini.Concurrent.Tasks;

namespace Lumini.Concurrent.Models
{
    public class ServiceConfiguration : IServiceConfiguration
    {
        public ServiceConfiguration()
        {
            Name = $"{Guid.NewGuid().ToString()}";
            IdleTimeInMilliseconds = 1000;
            NumberOfWorkers = 1;
            WorkerBatchSize = 100;
            PropagationDelayInMilliseconds = 1000;
            Enabled = true;
            ClassName = typeof(BasicServiceTask).FullName;
            AssemblyName = GetType().Assembly.GetName().Name;
        }

        public string Name { get; set; }
        public int IdleTimeInMilliseconds { get; set; }
        public int NumberOfWorkers { get; set; }
        public int WorkerBatchSize { get; set; }
        public bool Enabled { get; set; }
        public string ClassName { get; set; }
        public string AssemblyName { get; set; }
        public int PropagationDelayInMilliseconds { get; set; }
    }
}