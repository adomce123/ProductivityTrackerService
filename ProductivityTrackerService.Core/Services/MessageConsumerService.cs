using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MessageConsumerService> _logger;

        public MessageConsumerService(
            IServiceScopeFactory scopeFactory,
            ILogger<MessageConsumerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var serviceScope = _scopeFactory.CreateAsyncScope();

            var kafkaConsumer = serviceScope.ServiceProvider
                .GetRequiredService<IKafkaConsumer>();

            var messageProcessor = serviceScope.ServiceProvider
                .GetRequiredService<IMessageProcessor>();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var response = await kafkaConsumer.ConsumeMessageAsync(stoppingToken);
                    _logger.LogInformation($"Message consumed: {response?.Message?.Value}");

                    try
                    {
                        await messageProcessor.ProcessAsync(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Message processing failed with exception {ex}", ex.Message);
                    }
                    finally
                    {
                        if (!response.IsPartitionEOF)
                            kafkaConsumer.StoreMessageOffset(response);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"{nameof(MessageConsumerService)}.{nameof(ExecuteAsync)} threw an exception.");
            }
        }
    }
}
