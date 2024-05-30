﻿using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Core.DTOs;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class FailedEntriesProducerService : IKafkaProducer, IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly KafkaConsumerSettings _configuration;
        private readonly ILogger<FailedEntriesProducerService> _logger;

        public FailedEntriesProducerService(
            IOptions<KafkaSettings> options,
            ILogger<FailedEntriesProducerService> logger)
        {
            _configuration = options.Value.FailedDayEntryConsumerSettings;
            var bootstrapServers = _configuration.ConsumerConfig?.BootstrapServers;

            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<Null, string>(config).Build();
            _logger = logger;
        }

        public async Task ProduceAsync(IEnumerable<DayEntryDto> batch)
        {
            try
            {
                foreach (DayEntryDto failedMsg in batch)
                {
                    var message = new Message<Null, string>
                    {
                        Value = JsonSerializer.Serialize(failedMsg)
                    };

                    await _producer
                        .ProduceAsync(_configuration.Topic, message);

                    _logger.LogInformation($"Produced failed message: {message.Value}");
                }
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError($"Kafka produce error: {ex.Error.Reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kafka producer failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
