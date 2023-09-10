using Confluent.Kafka;

namespace ProductivityTrackerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _consumerConfig;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _consumerConfig = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

            consumer.Assign(new TopicPartitionOffset(topic, new Partition(0), Offset.Beginning));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var response = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);

                if (response.Message != null) 
                {
                    _logger.LogInformation($"Message consumed: {response.Message.Value}");
                    //_logger.LogInformation($"Message consumed: {message?.Id} {message?.Number}");
                }
            }
        }
    }
}