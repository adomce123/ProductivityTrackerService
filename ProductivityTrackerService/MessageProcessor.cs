using Confluent.Kafka;
using ProductivityTrackerService.Configuration;
using ProductivityTrackerService.Models;
using ProductivityTrackerService.Services;
using System.Text.Json;

namespace ProductivityTrackerService
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly IDayEntriesService _dayEntriesService;
        private readonly List<DayEntryDto> _dayEntriesList;
        private const int BatchSize = 5;

        public MessageProcessor(IDayEntriesService dayEntriesService)
        {
            _dayEntriesService = dayEntriesService;

            _dayEntriesList = new List<DayEntryDto>();
        }

        public async Task ProcessAsync(ConsumeResult<Null, string> response)
        {
            if (response.IsPartitionEOF && _dayEntriesList.Count == 0)
                return;

            if (response.IsPartitionEOF && _dayEntriesList.Count > 0)
            {
                await _dayEntriesService.InsertDayEntriesAsync(_dayEntriesList);

                _dayEntriesList.Clear();

                return;
            }

            if (_dayEntriesList.Count == BatchSize)
            {
                await _dayEntriesService.InsertDayEntriesAsync(_dayEntriesList);

                _dayEntriesList.Clear();
            }

            var dayEntry =
                JsonSerializer.Deserialize<DayEntryDto>(
                    response.Message.Value,
                    SerializerConfiguration.DefaultSerializerOptions)
                ?? throw new ArgumentException("Were not able to deserialize day entries");

            _dayEntriesList.Add(dayEntry);
        }
    }
}
