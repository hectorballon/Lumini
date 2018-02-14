using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace Lumini.Framework.Common
{
    public sealed class InMemoryCache<TValue> : InMemoryCache<object, TValue>, IMemoryCache<TValue>
        where TValue : class, new()
    {
        public InMemoryCache()
        {
        }

        public InMemoryCache(MemoryCacheEntryOptions cacheEntryOptions)
            : base(cacheEntryOptions)
        {
        }

        public void AddRange(IEnumerable<TValue> list)
        {
            foreach (var item in list)
                Add(item);
        }

        public void Add(TValue item)
        {
            var type = item.GetType();
            var properties = type.GetProperties();
            var property = properties
                .FirstOrDefault(p => p.GetCustomAttributes(false)
                    .Any(a => a is KeyAttribute));
            if (property == null) throw new ArgumentException("Class must have a parameter with Key attribute");
            this[property.GetValue(item)] = item;
        }
    }

    public sealed class InMemoryCacheWithStringKey<TValue> : InMemoryCache<string, TValue>
        where TValue : class, new()
    {
    }

    public class InMemoryCacheWithIntKey<TValue> : InMemoryCache<int, TValue>
        where TValue : class, new()
    {
    }

    public class InMemoryCache<TKey, TValue> : IMemoryCache<TKey, TValue>
        where TValue : class, new()
    {
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;

        public InMemoryCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public InMemoryCache(MemoryCacheEntryOptions cacheEntryOptions)
            : this()
        {
            _cacheEntryOptions = cacheEntryOptions ?? new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(15)
            };
#if DEBUG
            _cacheEntryOptions.RegisterPostEvictionCallback(InMemoryCacheHelper.PostEvictionCallback);
#endif
        }

        protected MemoryCache Cache { get; }

        public virtual TValue GetValue(TKey key)
        {
            return Cache.TryGetValue(key, out TValue o) ? o : null;
        }

        public virtual void SetValue(TKey key, TValue value, MemoryCacheEntryOptions options = null)
        {
            options = options ?? _cacheEntryOptions;
#if DEBUG
            if (options != _cacheEntryOptions)
                options.RegisterPostEvictionCallback(InMemoryCacheHelper.PostEvictionCallback);
#endif
            Cache.Set(key, value, options);
        }

        public TValue this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }
    }
}