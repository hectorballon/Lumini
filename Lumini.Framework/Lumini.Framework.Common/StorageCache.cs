using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lumini.Framework.Common
{
    public class StorageCache<TValue> : IStorageCache<TValue>
        where TValue : class, new()
    {
        private readonly Func<IEnumerable<TValue>> _getData;
        private readonly IList<TValue> _items;
        private readonly TimeSpan _lifeTime;
        private readonly IList<Expression<Func<TValue, object>>> _lookups;

        private volatile State _currentState = State.Empty;
        private Dictionary<string, ILookup<object, TValue>> _indexes;
        private DateTime _refreshedOn = DateTime.MinValue;
        private volatile object _stateLock = new object();

        public StorageCache(Func<IEnumerable<TValue>> getData, TimeSpan? lifeTime = null)
        {
            _lookups = new List<Expression<Func<TValue, object>>>();
            _indexes = new Dictionary<string, ILookup<object, TValue>>();
            _getData = getData;
            _items = new List<TValue>();
            _lifeTime = lifeTime ?? TimeSpan.FromMinutes(30);
            FirstLoad = true;
            CanBeRefreshed = true;
            Available = false;
        }

        public bool CanBeRefreshed { get; set; }
        public bool Available { get; private set; }
        public bool FirstLoad { get; private set; }

        public TValue GetItem<TProperty>(Expression<Func<TValue, TProperty>> property, TProperty value)
        {
            switch (_currentState)
            {
                case State.OnLine:
                    var timeSpentInCache = DateTime.UtcNow - _refreshedOn;
                    if (timeSpentInCache > _lifeTime)
                        Task.Factory.StartNew(Refresh).ContinueWith(_ => Available = true);
                    break;

                case State.Empty:
                    Task.Factory.StartNew(Refresh).ContinueWith(_ =>
                    {
                        Available = true;
                        FirstLoad = false;
                    });
                    break;
            }

            return FindValue(property, value);
        }

        public void Invalidate()
        {
            while (!CanBeRefreshed) Thread.Sleep(5000);
            lock (_stateLock)
            {
                Available = false;
                _refreshedOn = DateTime.MinValue;
                _currentState = State.OnLine;
            }
        }

        public void Add(TValue item)
        {
            _items.Add(item);
        }

        public void AddRange(IEnumerable<TValue> list)
        {
            foreach (var entry in list)
                Add(entry);
        }

        public void AddIndex(Expression<Func<TValue, object>> property)
        {
            _lookups.Add(property);
        }

        private void RebuildIndexes()
        {
            _indexes = _lookups.ToDictionary(
                lookup => lookup.ToString(), lookup => _items.ToLookup(lookup.Compile()));
        }

        private TValue FindValue<TProperty>(Expression<Func<TValue, TProperty>> property, TProperty value)
        {
            var key = property.ToString();
            if (_indexes.ContainsKey(key))
                return _indexes[key][value].FirstOrDefault();
            var c = property.Compile();
            return _items.FirstOrDefault(x => Equals(c(x), value));
        }

        private void Refresh()
        {
            lock (_stateLock)
            {
                _currentState = State.Refreshing;
                var data = _getData();
                _items.Clear();
                AddRange(data);
                RebuildIndexes();
                _refreshedOn = DateTime.UtcNow;
                _currentState = State.OnLine;
            }
        }

        private enum State
        {
            Empty,
            OnLine,
            Refreshing
        }
    }
}