using ProductivityTrackerService.Core.Interfaces;
using System.Collections.Concurrent;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class ConcurrentQueueWrapper<T> : IQueue<T>
    {
        private readonly ConcurrentQueue<T> _concurrentQueue = new();
        public int Count => _concurrentQueue.Count;

        public bool TryDequeue(out T message)
        {
            return _concurrentQueue.TryDequeue(out message);
        }

        public void Enqueue(T message)
        {
            _concurrentQueue.Enqueue(message);
        }
    }
}
