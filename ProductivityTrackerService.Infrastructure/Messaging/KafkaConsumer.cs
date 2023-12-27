using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Core.Configuration;
using ProductivityTrackerService.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Messaging
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
            var consumeResult = await Task.Run(() => _consumer
                .Consume(stoppingToken), stoppingToken);
            return consumeResult;
        }

        public void StoreMessageOffset(ConsumeResult<Null, string> consumeResult)
        {
            _consumer.StoreOffset(consumeResult);
        }
    }
}
