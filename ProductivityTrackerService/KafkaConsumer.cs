using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Configuration;

namespace ProductivityTrackerService
{
    public class KafkaConsumer : IKafkaConsumer
    {
        private readonly IConsumer<Null, string> _consumer;

        public KafkaConsumer(IOptions<ConsumerConfiguration> options)
        {
            var configuration = options.Value;

            _consumer = new ConsumerBuilder<Null, string>(configuration.ConsumerConfig)
                .Build();

            _consumer.Subscribe(configuration.Topic);
        }

        public async Task<ConsumeResult<Null, string>> ConsumeMessageAsync(
            CancellationToken stoppingToken)
        {
            return await Task.Run(() => _consumer
                .Consume(stoppingToken), stoppingToken);
        }

        public void StoreMessageOffset(ConsumeResult<Null, string> consumeResult)
        {
            _consumer.StoreOffset(consumeResult);
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }
    }
}
