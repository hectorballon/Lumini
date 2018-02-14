using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lumini.Framework.Dataflow
{
    public interface IDataProvider<T>
        where T : class
    {
        LinkedList<IDataSource<T>> DataSources { get; }
        void AddDataSources(params IDataSource<T>[] dataSources);
        Task<T> GetNextItemAsync(CancellationToken cancellationToken);
        void AddResetEvent(DataAvailableResetEvent resetEvent);

        TDataSource CreateDataSource<TDataSource>(string dataSourceName, ushort priority = ushort.MaxValue)
            where TDataSource : IDataSource<T>;
    }
}