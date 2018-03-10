using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Lumini.Concurrent;
using Lumini.Concurrent.Tasks;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Lumini.App
{
    static class Program
    {
        static void Main()
        {
            var services = new ServiceCollection();
            services.RegisterServices();
            var serviceProvider = services.BuildServiceProvider();

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var factory = ActivatorUtilities.CreateInstance<ServiceFactory>(serviceProvider);
            //var task1 = (BasicServiceTask)serviceProvider.GetInstanceFor(Constants.Services.StagingService, factory);
            //task1.Execute = DoWork;
            var task2 = (RoundRobinServiceTask)serviceProvider.GetInstanceFor(Constants.Services.CleansingService, factory);
            task2.ProcessItem = ProcessItem;
            Task.WaitAll(
                //task1.Start(cts.Token),
                task2.Start(cts.Token),
                Task.Run(async () =>
                 {
                     while (true)
                     {
                         if (cts.Token.IsCancellationRequested) break;
                         task2.Enqueue(DateTime.UtcNow);
                         await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
                     }
                 }, cts.Token)
                );
        }

        private static IServiceTask GetInstanceFor(this IServiceProvider serviceProvider, string serviceName, ServiceFactory factory)
        {
            var configuration = factory.GetSettingsFor(serviceName);
            var typeName = configuration.ClassName;
            if (!string.IsNullOrEmpty(configuration.AssemblyName))
                typeName = $"{typeName}, {configuration.AssemblyName}";
            var logger = serviceProvider.GetService<Common.ILogger>();
            var type = Type.GetType(typeName);
            var service = (IServiceTask)Activator.CreateInstance(type, configuration, logger);
            return service;
        }

        private static async Task<bool> ProcessItem(object item, int instanceid)
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"En ejecucion... Worker Id:{instanceid} - {item.ToString()} - {DateTime.Now.ToString(CultureInfo.InvariantCulture)}");
                return true;
            });
        }

        private static async Task DoWork()
        {
            await Task.Run(() =>
                Console.WriteLine($"En ejecucion... {DateTime.Now.ToString(CultureInfo.InvariantCulture)}"));
        }
    }
}
