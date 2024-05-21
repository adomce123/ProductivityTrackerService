using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Core.Configuration;
using ProductivityTrackerService.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class KafkaConsumer : IKafkaConsumer
    {
        private readonly IConsumer<Null, string> _consumer;
        private readonly ILogger<KafkaConsumer> _logger;

        public KafkaConsumer(IOptions<ConsumerConfiguration> options, ILogger<KafkaConsumer> logger)
        {
            var configuration = options.Value;

            _consumer = new ConsumerBuilder<Null, string>(configuration.ConsumerConfig)
                .SetLogHandler((_, logMessage) =>
                {
                    logger.LogInformation($"Kafka log: {logMessage.Message}");
                })
                .SetErrorHandler((_, error) =>
                {
                    logger.LogError($"Kafka error: {error.Reason}");
                })
                .Build();

            _consumer.Subscribe(configuration.Topic);
            _logger = logger;
        }

        public Task<ConsumeResult<Null, string>?> ConsumeMessageAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var consumeResult = _consumer.Consume(ct);
                if (consumeResult != null)
                {
                    return Task.FromResult<ConsumeResult<Null, string>?>(consumeResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Error in {Consumer}: {Message}", typeof(KafkaConsumer), ex.Message);
            }

            return Task.FromResult<ConsumeResult<Null, string>?>(null);
        }

        public void StoreMessageOffset(ConsumeResult<Null, string> consumeResult)
        {
            try
            {
                _consumer.StoreOffset(consumeResult);
            }
            catch (KafkaException ex)
            {
                _logger.LogError(ex, "Error storing offset: {Message}", ex.Message);
            }
        }
    }
}
