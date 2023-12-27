using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductivityTrackerService.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Services
{
    public class EntryPointService(
        IServiceScopeFactory _scopeFactory, ILogger<EntryPointService> _logger) : IEntryPointService
    {
        public async Task ExecuteAsync()
        {
            await using var serviceScope = _scopeFactory.CreateAsyncScope();

            var kafkaConsumer = serviceScope.ServiceProvider
                .GetRequiredService<IKafkaConsumer>();

            var messageProcessor = serviceScope.ServiceProvider
                .GetRequiredService<IMessageProcessor>();

            try
            {
                var cts = new CancellationTokenSource();
                var response = await kafkaConsumer.ConsumeMessageAsync(cts.Token);
                if (response.Value != null)
                    _logger.LogInformation("Message consumed: {response.Value}", response.Value);

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
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"{nameof(EntryPointService)}.{nameof(ExecuteAsync)} threw an exception.");
            }
        }
    }
}