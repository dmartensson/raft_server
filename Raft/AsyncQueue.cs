using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Raft
{
    public class AsyncQueue<T>
    {
        private readonly SemaphoreSlim _sem;
        private readonly ConcurrentQueue<T> _que;

        public AsyncQueue(ConcurrentQueue<T> queue = null)
        {
            _sem = new SemaphoreSlim(0);
            _que = queue ?? new ConcurrentQueue<T>();
        }

        public void Enqueue(T item)
        {
            _que.Enqueue(item);
            _sem.Release();
        }

        public void EnqueueRange(IEnumerable<T> source)
        {
            var n = 0;
            foreach (var item in source)
            {
                _que.Enqueue(item);
                n++;
            }
            _sem.Release(n);
        }

        public async Task<T> DequeueAsync(int timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _sem.WaitAsync(timeout, cancellationToken);

            if (_que.TryDequeue(out var item))
            {
                return item;
            }

            return default(T);
        }
    }
}