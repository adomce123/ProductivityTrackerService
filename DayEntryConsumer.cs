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
        private readonly ConsumerConfiguration _consumerConfiguration;
        private readonly IDayEntriesService _dayEntriesService;

        public DayEntryConsumer(
            ILogger<DayEntryConsumer> logger, 
            ConsumerConfiguration consumerConfiguration,
            IDayEntriesService dayEntriesService)
        {
            _logger = logger;
            _consumerConfiguration = consumerConfiguration;
            _dayEntriesService = dayEntriesService;
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

                        var dayEntry = 
                            JsonSerializer.Deserialize<DayEntryDto>(response.Message.Value)
                            ?? throw new ArgumentException("Were not able to deserialize day entries");

                        var dayEntriesList = new List<DayEntryDto> { dayEntry };

                        var lala = await _dayEntriesService.GetDayEntriesAsync();

                        //await _dayEntriesService.InsertDayEntriesAsync(dayEntriesList);
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