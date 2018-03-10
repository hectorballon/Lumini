using System;

namespace Lumini.Concurrent
{
    public class ServiceFactory
    {
        private readonly Func<string, IServiceConfiguration> _factory;

        public ServiceFactory(Func<string, IServiceConfiguration> factory)
        {
            _factory = factory;
        }

        public IServiceConfiguration GetSettingsFor(string serviceName)
        {
            return _factory(serviceName);
        }
    }
}