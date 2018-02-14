using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Lumini.Framework.Dataflow
{
    public class StackDataSource<T> : QueueDataSource<T>, IDataSource<T>
        where T : class
    {
        protected internal StackDataSource(string dataSourceName, IDataProvider<T> dataProvider, ushort priority)
            : base(dataSourceName, dataProvider, priority)
        {
        }

        protected override IProducerConsumerCollection<T> CreateBuffer()
        {
            return new ConcurrentStack<T>();
        }

        public virtual async Task<T> Push(T item)
        {
            LoadItem(item);
            await Task.Run(() => ((ConcurrentStack<T>) InternalBuffer).Push(item));
            return item;
        }
    }
}