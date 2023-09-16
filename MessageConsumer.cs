using Confluent.Kafka;
using ProductivityTrackerService.Configuration;

namespace ProductivityTrackerService
{
    public class MessageConsumer : BackgroundService
    {
        private readonly ILogger<MessageConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MessageConsumer(
            ILogger<MessageConsumer> logger, 
            IConfiguration consumerConfiguration,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _configuration = consumerConfiguration;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumerConfiguration = _configuration.GetSection("DayEntriesConsumer")
                .Get<ConsumerConfiguration>()
                ?? throw new ArgumentException("Were not able to Consumer configuration");

            using var consumer =
                new ConsumerBuilder<Null, string>(consumerConfiguration.ConsumerConfig).Build();

            consumer.Subscribe(consumerConfiguration.Topic);

            using var scope = _serviceScopeFactory.CreateScope();

            var messageProcessor = scope.ServiceProvider.GetService<IMessageProcessor>()
                ?? throw new ArgumentException("Were not able to create scoped Message processor");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var response = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken); // what is this

                    _logger.LogInformation($"Message consumed: {response.Message}");

                    await messageProcessor.ProcessAsync(response);
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