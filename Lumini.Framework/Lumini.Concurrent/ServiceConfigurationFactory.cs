using System.Collections.Generic;
using System.IO;
using Lumini.Concurrent.Models;
using Microsoft.Extensions.Configuration;

namespace Lumini.Concurrent
{
    public static class ServiceConfigurationFactory
    {
        public const string SettingsRootSectionName = "Lumini.Concurrent";

        static ServiceConfigurationFactory()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public static ServiceSettings<T> GetSettings<T>(string key = "")
            where T : IServiceConfiguration, new()
        {
            var section = Configuration?.GetSection(SettingsRootSectionName);
            if (!string.IsNullOrEmpty(key)) section = section?.GetSection(key);
            var settings = new ServiceSettings<T>();
            section.Bind(settings);
            return settings;
        }

        public static IConfigurationRoot Configuration { get; }

        public sealed class ServiceSettings<T>
        {
            public List<T> Services { get; set; }
        }
    }
}