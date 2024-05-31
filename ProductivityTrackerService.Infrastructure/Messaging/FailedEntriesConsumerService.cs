using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Messaging
{
    public class FailedEntriesConsumerService : BackgroundService
    {
        private readonly ILogger<FailedEntriesConsumerService> _logger;
        private readonly IConsumer<Null, string> _consumer;
        private readonly IMessageProcessorService _messageProcessor;

        public FailedEntriesConsumerService(
            IMessageProcessorService messageProcessor,
            ILogger<FailedEntriesConsumerService> logger,
            IOptions<KafkaSettings> configuration)
        {
            _messageProcessor = messageProcessor;
            _logger = logger;

            var consumerSettings = configuration.Value.FailedDayEntryConsumerSettings;

            _consumer = new ConsumerBuilder<Null, string>(consumerSettings.ConsumerConfig).Build();
            _consumer.Subscribe(consumerSettings.Topic);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{typeof(FailedEntriesConsumerService)} service started.");

            var consumerTask = Task.Run(() => RunConsumerLoop(stoppingToken));

            await consumerTask;
        }

        private async Task RunConsumerLoop(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var response = _consumer.Consume(stoppingToken);

                    if (response != null)
                    {
                        _logger.LogInformation($"{typeof(FailedEntriesConsumerService)} consumed: {response.Message?.Value}");

                        try
                        {
                            await _messageProcessor.ProcessAsync(response, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{typeof(FailedEntriesConsumerService)} message " +
                                $"processing failed with exception: {ex.Message}");
                        }
                        finally
                        {
                            if (!response.IsPartitionEOF)
                            {
                                _consumer.StoreOffset(response);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"{typeof(FailedEntriesConsumerService)} operation was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(FailedEntriesConsumerService)}.{nameof(ExecuteAsync)} threw an exception.");
            }
            finally
            {
                await _messageProcessor.HandleNotProcessedMessages();
                _logger.LogInformation($"{typeof(FailedEntriesConsumerService)} is stopping.");
                _consumer.Close();
            }
        }
    }
}
