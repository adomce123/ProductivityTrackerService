﻿using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Messaging.Base;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class FailedEntriesConsumerService : BaseConsumer
    {
        public FailedEntriesConsumerService(
            IMessageProcessorService messageProcessor,
            ILogger<FailedEntriesConsumerService> logger,
            string? topicName,
            IConsumer<int, string> consumer)
            : base(messageProcessor, logger, topicName, consumer)
        {
        }
    }
}
