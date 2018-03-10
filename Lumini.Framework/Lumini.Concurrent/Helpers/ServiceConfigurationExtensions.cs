using System.Linq;

namespace Lumini.Concurrent.Helpers
{
    public static class ServiceConfigurationExtensions
    {
        public static T GetConfiguration<T>(
            this ServiceConfigurationFactory.ServiceSettings<T> settings,
            string name)
            where T : IServiceConfiguration, new()
        {
            return settings.Services.Exists(t => t.Name.Equals(name)) ?
                settings.Services.FirstOrDefault(t => t.Name.Equals(name)) :
                DefaultServiceConfiguration<T>(name);
        }

        private static T DefaultServiceConfiguration<T>(string name)
            where T : IServiceConfiguration, new()
        {
            return new T
            {
                Name = name
            };
        }
    }
}