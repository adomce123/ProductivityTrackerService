using Confluent.Kafka;

namespace ProductivityTrackerService
{
    public class DayEntryConsumer : BackgroundService
    {
        private readonly ILogger<DayEntryConsumer> _logger;
        private readonly IConfiguration _consumerConfig;

        public DayEntryConsumer(ILogger<DayEntryConsumer> logger, IConfiguration config)
        {
            _logger = logger;
            _consumerConfig = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var topic = _consumerConfig.GetValue<string>("Topic");

                var consumerConfig = new ConsumerConfig
                {
                    GroupId = _consumerConfig.GetValue<string>("ConsumerName"),
                    BootstrapServers = _consumerConfig.GetValue<string>("Broker"),
                    AutoOffsetReset = AutoOffsetReset.Latest
                };

                using var consumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();
                consumer.Subscribe(topic);

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