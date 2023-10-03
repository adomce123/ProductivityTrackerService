using Confluent.Kafka;
using ProductivityTrackerService.Configuration;

namespace ProductivityTrackerService
{
    public class MessageConsumer : BackgroundService
    {
        private readonly ILogger<MessageConsumer> _logger;
        private readonly ConsumerConfiguration _consumerConfiguration;
        private readonly IMessageProcessor _messageProcessor;
        private readonly IConsumer<Null, string> _consumer;

        public MessageConsumer(
            ILogger<MessageConsumer> logger, 
            IConfiguration configuration,
            IMessageProcessor messageProcessor)
        {
            _logger = logger;
            _messageProcessor = messageProcessor;

            _consumerConfiguration = configuration.GetSection("DayEntriesConsumer")
                .Get<ConsumerConfiguration>()
                ?? throw new ArgumentException("Were not able to Consumer configuration");

            _consumer = new ConsumerBuilder<Null, string>(_consumerConfiguration.ConsumerConfig)
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_consumerConfiguration.Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var response = await Task.Run(() => _consumer
                        .Consume(stoppingToken), stoppingToken);

                    _logger.LogInformation("Message consumed: {response.Message}", response.Message);

                    try
                    {
                        await _messageProcessor.ProcessAsync(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Message processing failed with exception {ex}", ex.Message);
                    }
                    finally
                    {
                        if (!response.IsPartitionEOF)
                            StoreOffset(response);
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
            finally
            {
                _consumer.Dispose();
            }
        }

        private void StoreOffset(ConsumeResult<Null, string> message)
        {
            _consumer.StoreOffset(message);
        }
    }
}