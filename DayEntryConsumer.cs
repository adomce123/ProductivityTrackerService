using Confluent.Kafka;
using ProductivityTrackerService.Configuration;

namespace ProductivityTrackerService
{
    public class DayEntryConsumer : BackgroundService
    {
        private readonly ILogger<DayEntryConsumer> _logger;
        private readonly ConsumerConfiguration _consumerConfiguration;

        public DayEntryConsumer(
            ILogger<DayEntryConsumer> logger, ConsumerConfiguration consumerConfiguration)
        {
            _logger = logger;
            _consumerConfiguration = consumerConfiguration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var consumer = 
                    new ConsumerBuilder<Null, string>(_consumerConfiguration.ConsumerConfig).Build();
                
                consumer.Subscribe(_consumerConfiguration.Topic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    var response = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);

                    if (response.Message != null)
                    {
                        _logger.LogInformation($"Message consumed: {response.Message.Value}");
                    }
                }
            }
            catch (OperationCanceledException) 
            {
                _logger.LogInformation("Operation was cancelled");
            }
            catch (Exception ex) 
            {
                _logger.LogCritical(ex, "A critical exception was thrown.");
            }
        }
    }
}