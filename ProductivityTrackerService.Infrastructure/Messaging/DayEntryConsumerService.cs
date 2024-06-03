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
    public class DayEntryConsumerService : BackgroundService
    {
        private readonly ILogger<DayEntryConsumerService> _logger;
        private readonly IMessageProcessorService _messageProcessor;
        private readonly IConsumer<Null, string> _consumer;

        public DayEntryConsumerService(
            IMessageProcessorService messageProcessor,
            ILogger<DayEntryConsumerService> logger,
            IOptions<KafkaSettings> options)
        {
            _messageProcessor = messageProcessor;
            _logger = logger;
            var consumerSettings = options.Value.DayEntryConsumerSettings;


            _consumer.Subscribe(consumerSettings.Topic);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{typeof(DayEntryConsumerService)} service started.");

            var consumerTask = Task.Run(() => RunConsumerLoop(stoppingToken), stoppingToken);

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
                        _logger.LogInformation($"{typeof(DayEntryConsumerService)} consumed: {response.Message?.Value}");

                        try
                        {
                            await _messageProcessor.ProcessAsync(response, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{typeof(DayEntryConsumerService)} message " +
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
                _logger.LogInformation($"{typeof(DayEntryConsumerService)} operation was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(DayEntryConsumerService)}.{nameof(ExecuteAsync)} threw an exception.");
            }
            finally
            {
                _consumer.Close();
                await _messageProcessor.HandleNotProcessedMessages();
                _logger.LogInformation($"{typeof(DayEntryConsumerService)} is stopping.");
            }
        }
    }
}
