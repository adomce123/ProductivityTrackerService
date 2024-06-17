namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IQueue<T>
    {
        void Enqueue(T message);
        bool TryDequeue(out T message);
        int Count { get; }
    }
}
