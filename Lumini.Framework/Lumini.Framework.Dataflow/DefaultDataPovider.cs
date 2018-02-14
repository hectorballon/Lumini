using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lumini.Framework.Dataflow
{
    public sealed class DefaultDataPovider<T> : IDataProvider<T>
        where T : class
    {
        private readonly List<DataAvailableResetEvent> _dataSourceWaitHandleList;
        private readonly int _idleTimeInSeconds;
        private IDataSource<T> _currentDataSource;

        public DefaultDataPovider(int idleTimeinSeconds = 10)
        {
            _idleTimeInSeconds = idleTimeinSeconds;
            DataSources = new LinkedList<IDataSource<T>>();
            _dataSourceWaitHandleList = new List<DataAvailableResetEvent>();
        }

        public LinkedList<IDataSource<T>> DataSources { get; }

        public TDataSource CreateDataSource<TDataSource>(string dataSourceName, ushort priority = ushort.MaxValue)
            where TDataSource : IDataSource<T>
        {
            return (TDataSource) Activator.CreateInstance(typeof(TDataSource), dataSourceName, this, priority);
        }

        public void AddDataSources(params IDataSource<T>[] dataSources)
        {
            foreach (var dataSource in dataSources)
                DataSources.AddLast(dataSource);
        }

        public async Task<T> GetNextItemAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            _currentDataSource = (IDataSource<T>) await SelectDataSourceForInput(cancellationToken);
            if (_currentDataSource == null)
                return null;

            var item = await _currentDataSource.GetNextItemAsync();
            if (item == null && !_currentDataSource.HasItemsInQueue)
                _currentDataSource.Reset();
            return item;
        }

        public void AddResetEvent(DataAvailableResetEvent resetEvent)
        {
            _dataSourceWaitHandleList.Add(resetEvent);
        }

        private async Task<IPrioritizedSource> SelectDataSourceForInput(CancellationToken cancellationToken)
        {
            var waitHandleArray = GetLocalCopyOfAvailableWaitHandleList(cancellationToken);
            var eventThatSignaledIndex =
                WaitHandle.WaitAny(waitHandleArray,
                    new TimeSpan(0, 0, _idleTimeInSeconds));

            if (eventThatSignaledIndex == WaitHandle.WaitTimeout)
                return ((DataAvailableResetEvent) waitHandleArray[0]).DataSource;
            if (!cancellationToken.IsCancellationRequested)
                return ((DataAvailableResetEvent) waitHandleArray[eventThatSignaledIndex]).DataSource;
            await WaitForCompletion();
            cancellationToken.ThrowIfCancellationRequested();

            return ((DataAvailableResetEvent) waitHandleArray[eventThatSignaledIndex]).DataSource;
        }

        private async Task WaitForCompletion()
        {
            var tasks = DataSources.Select(dataSource => dataSource.WaitForCompletion()).ToList();
            await Task.WhenAll(tasks);
        }

        private WaitHandle[] GetLocalCopyOfAvailableWaitHandleList(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();
            var localCopy = new List<WaitHandle>();
            localCopy.AddRange(_dataSourceWaitHandleList.OrderBy(i => i.Priority));
            localCopy.Add(token.WaitHandle);
            var waitHandleArray = new WaitHandle[localCopy.Count];
            localCopy.CopyTo(waitHandleArray);
            return waitHandleArray;
        }
    }
}