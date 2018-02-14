using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Lumini.Framework.Common
{
    public interface IStorageCache<TValue>
        where TValue : class, new()
    {
        void Add(TValue item);
        void AddRange(IEnumerable<TValue> list);
        TValue GetItem<TProperty>(Expression<Func<TValue, TProperty>> property, TProperty value);
        void Invalidate();
    }
}