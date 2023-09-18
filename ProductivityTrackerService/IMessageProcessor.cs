using Confluent.Kafka;

namespace ProductivityTrackerService
{
    public interface IMessageProcessor
    {
        Task ProcessAsync(ConsumeResult<Null, string> response);
    }
}
