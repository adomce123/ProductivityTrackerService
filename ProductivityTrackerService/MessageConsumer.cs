using ProductivityTrackerService.Core.Interfaces;

namespace ProductivityTrackerService
{
    public class MessageConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MessageConsumer> _logger;

        public MessageConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<MessageConsumer> logger)
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
                    _logger.LogInformation("Message consumed: {response.Message}", response.Message);

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
                _logger.LogCritical(ex.Message, "A critical exception was thrown. Discarding message");
            }
        }
    }
}