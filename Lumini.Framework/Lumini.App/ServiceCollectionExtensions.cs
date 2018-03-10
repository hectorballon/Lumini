using System;
using Lumini.App.Log;
using Lumini.Common;
using Lumini.Concurrent;
using Lumini.Concurrent.Helpers;
using Lumini.Concurrent.Models;
using Lumini.Concurrent.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Lumini.App
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterServices(
            this IServiceCollection services)
        {
            services.AddTransient<Func<string, IServiceConfiguration>>(factory =>
            {
                return key => ServiceConfigurationFactory
                    .GetSettings<ServiceConfiguration>(Constants.ConfigurationSection)
                    .GetConfiguration(key);
            });
            services.AddTransient<BasicServiceTask>();
            services.AddSingleton<ILogger, Logger4NetAdapter>();
            return services;
        }

    }
}