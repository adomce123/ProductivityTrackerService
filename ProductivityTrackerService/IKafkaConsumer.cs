using Confluent.Kafka;

namespace ProductivityTrackerService
{
    public interface IKafkaConsumer
    {
        Task<ConsumeResult<Null, string>> ConsumeMessageAsync(CancellationToken stoppingToken);
        void StoreMessageOffset(ConsumeResult<Null, string> consumeResult);
    }
}
