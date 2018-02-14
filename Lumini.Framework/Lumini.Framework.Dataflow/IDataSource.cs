using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lumini.Framework.Dataflow
{
    public delegate Task DataSourceEventHandler(object dataSource, DataSourceEventArgs<object> args);

    public interface IDataSource<T> : IPrioritizedSource, IDisposable
        where T : class
    {
        bool HasItemsInQueue { get; }
        Task<T> GetNextItemAsync();

        void Stop();
        void Start();
        void GetDataFrom(Func<int, IQueryable<T>> method);
        void Reset();
        Task WaitForCompletion();

        event DataSourceEventHandler ItemAvailableforProcessing;
        event DataSourceEventHandler DataFound;
    }
}