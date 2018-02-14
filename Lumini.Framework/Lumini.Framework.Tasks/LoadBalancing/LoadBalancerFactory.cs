namespace Lumini.Framework.Tasks.LoadBalancing
{
    public static class LoadBalancerFactory
    {
        public static LoadBalancerBuilder<T> Create<T>()
            where T : class, ILoadBalancer, new()
        {
            return new LoadBalancerBuilder<T>().Create();
        }

        public class LoadBalancerBuilder<T>
            where T : class, ILoadBalancer, new()
        {
            private ILoadBalancer _loadBalancer;
            private LoadBalancingSettings _settings;

            internal LoadBalancerBuilder()
            {
            }

            internal LoadBalancerBuilder<T> Create()
            {
                _loadBalancer = new T();
                return this;
            }

            public LoadBalancerBuilder<T> WithSettings(LoadBalancingSettings settings)
            {
                _settings = settings;
                return this;
            }

            public ILoadBalancer Build()
            {
                _settings = _settings ?? new LoadBalancingSettings();
                _settings.LoadBalancer = _loadBalancer;
                return _loadBalancer;
            }
        }
    }
}