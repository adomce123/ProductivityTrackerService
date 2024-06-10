using Confluent.Kafka;
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
        private readonly IProducer<int, string> _producer;
        private readonly KafkaConsumerSettings _configuration;
        private readonly ILogger<FailedEntriesProducerService> _logger;
        private bool _disposed = false;

        public FailedEntriesProducerService(
            IOptions<KafkaSettings> options,
            ILogger<FailedEntriesProducerService> logger)
        {
            _configuration = options.Value.FailedDayEntryConsumerSettings;
            var bootstrapServers = _configuration.ConsumerConfig?.BootstrapServers;

            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<int, string>(config).Build();
            _logger = logger;
        }

        public async Task ProduceAsync(IEnumerable<DayEntryDto> batch)
        {
            try
            {
                foreach (DayEntryDto failedMsg in batch)
                {
                    var message = new Message<int, string>
                    {
                        Key = 2,
                        Value = JsonSerializer.Serialize(failedMsg)
                    };

                    await _producer
                        .ProduceAsync(_configuration.Topic, message);

                    _logger.LogInformation($"Produced failed message: {message.Value}");
                }
            }
            catch (ProduceException<int, string> ex)
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _producer?.Dispose();
                }

                // Dispose unmanaged resources here if any

                _disposed = true;
            }
        }
    }
}
