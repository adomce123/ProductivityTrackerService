using Confluent.Kafka;
using ProductivityTrackerService.Configuration;
using ProductivityTrackerService.Models;
using ProductivityTrackerService.Services;
using System.Text.Json;

namespace ProductivityTrackerService
{
    public class DayEntryConsumer : BackgroundService
    {
        private readonly ILogger<DayEntryConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DayEntryConsumer(
            ILogger<DayEntryConsumer> logger, 
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
                .Get<ConsumerConfiguration>();

            try
            {
                using var consumer = 
                    new ConsumerBuilder<Null, string>(consumerConfiguration.ConsumerConfig).Build();
                
                consumer.Subscribe(consumerConfiguration.Topic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    using var scope = _serviceScopeFactory.CreateScope();

                    var dayEntriesService = scope.ServiceProvider.GetService<IDayEntriesService>()
                        ?? throw new ArgumentException("Were not able to create scoped DayEntriesService");

                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    var response = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken); // what is this

                    if (response.Message != null)
                    {
                        _logger.LogInformation($"Message consumed: {response.Message.Value}");

                        var dayEntry = 
                            JsonSerializer.Deserialize<DayEntryDto>(
                                response.Message.Value,
                                SerializerConfiguration.DefaultSerializerOptions)
                            ?? throw new ArgumentException("Were not able to deserialize day entries");

                        var dayEntriesList = new List<DayEntryDto> { dayEntry };

                        await dayEntriesService.InsertDayEntriesAsync(dayEntriesList);
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