using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace Lumini.Framework.Common
{
    public interface IMemoryCache<TValue> : IMemoryCache<object, TValue>
        where TValue : class, new()
    {
        void Add(TValue item);
        void AddRange(IEnumerable<TValue> list);
    }

    public interface IMemoryCache<in TKey, TValue>
        where TValue : class, new()
    {
        TValue this[TKey key] { get; set; }
        TValue GetValue(TKey key);
        void SetValue(TKey key, TValue value, MemoryCacheEntryOptions options = null);
    }
}