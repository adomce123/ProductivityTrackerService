namespace ProductivityTrackerService
{
    public class MessageConsumer : BackgroundService
    {
        private readonly ILogger<MessageConsumer> _logger;
        private readonly IMessageProcessor _messageProcessor;
        private readonly IKafkaConsumer _kafkaConsumer;

        public MessageConsumer(
            ILogger<MessageConsumer> logger,
            IMessageProcessor messageProcessor,
            IKafkaConsumer kafkaConsumer)
        {
            _logger = logger;
            _messageProcessor = messageProcessor;
            _kafkaConsumer = kafkaConsumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var response = await _kafkaConsumer.ConsumeMessageAsync(stoppingToken);
                    _logger.LogInformation("Message consumed: {response.Message}", response.Message);

                    try
                    {
                        await _messageProcessor.ProcessAsync(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Message processing failed with exception {ex}", ex.Message);
                    }
                    finally
                    {
                        if (!response.IsPartitionEOF)
                            _kafkaConsumer.StoreMessageOffset(response);
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
                _kafkaConsumer.DisposeConsumer();
            }
        }
    }
}