#if DEBUG
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace Lumini.Framework.Common
{
    public static class InMemoryCacheHelper
    {
        public static void PostEvictionCallback(object key,
            object value, EvictionReason reason, object state)
        {
            var message = $"Cache entry {key} => {value} was removed : {reason}";
            //_theLock.TryRemove(key: (TKey)key, value: out TValue o);
            Debug.WriteLine(message);
        }
    }
}
#endif