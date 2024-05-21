using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductivityTrackerService.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Services
{
    public class MessageConsumerService : BackgroundService
    {
        private readonly ILogger<MessageConsumerService> _logger;
        private readonly IKafkaConsumer _kafkaConsumer;
        private readonly IMessageProcessor _messageProcessor;

        public MessageConsumerService(
            IKafkaConsumer kafkaConsumer,
            IMessageProcessor messageProcessor,
            ILogger<MessageConsumerService> logger)
        {
            _kafkaConsumer = kafkaConsumer;
            _messageProcessor = messageProcessor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MessageConsumerService started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var response = await _kafkaConsumer.ConsumeMessageAsync(stoppingToken);
                    if (response != null)
                    {
                        _logger.LogInformation($"Message consumed: {response.Message?.Value}");

                        try
                        {
                            await _messageProcessor.ProcessAsync(response, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Message processing failed with exception: {Message}", ex.Message);
                        }
                        finally
                        {
                            if (!response.IsPartitionEOF)
                            {
                                _kafkaConsumer.StoreMessageOffset(response);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MessageConsumerService operation was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(MessageConsumerService)}.{nameof(ExecuteAsync)} threw an exception.");
            }
            finally
            {
                _logger.LogInformation("MessageConsumerService is stopping.");
            }
        }
    }
}