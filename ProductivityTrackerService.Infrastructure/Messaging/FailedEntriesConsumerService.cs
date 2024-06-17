using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Configuration;
using ProductivityTrackerService.Infrastructure.Messaging.ProductivityTrackerService.Infrastructure.Messaging;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class FailedEntriesConsumerService : BaseConsumer
    {
        public FailedEntriesConsumerService(
            IMessageProcessorService messageProcessor,
            ILogger<FailedEntriesConsumerService> logger,
            IOptions<KafkaSettings> options,
            IConsumer<int, string> consumer)
            : base(messageProcessor, logger, options.Value.FailedDayEntryConsumerSettings, consumer)
        { }
    }
}
