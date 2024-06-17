using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using ProductivityTrackerService.Core.Interfaces;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class DayEntryConsumerService : BaseConsumer
    {
        public DayEntryConsumerService(
            IMessageProcessorService messageProcessor,
            ILogger<DayEntryConsumerService> logger,
            string? topicName,
            IConsumer<int, string> consumer)
            : base(messageProcessor, logger, topicName, consumer)
        {
        }
    }
}
