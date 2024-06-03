using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Configuration;
using ProductivityTrackerService.Infrastructure.Messaging.ProductivityTrackerService.Infrastructure.Messaging;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class DayEntryConsumerService : BaseConsumer
    {
        public DayEntryConsumerService(
            IMessageProcessorService messageProcessor,
            ILogger<DayEntryConsumerService> logger,
            IOptions<KafkaSettings> options)
            : base(messageProcessor, logger, options.Value.DayEntryConsumerSettings)
        { }
    }
}
