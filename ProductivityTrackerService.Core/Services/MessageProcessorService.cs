using Confluent.Kafka;
using ProductivityTrackerService.Core.Configuration;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProductivityTrackerService
{
    public class MessageProcessorService : IMessageProcessor
    {
        private readonly IDayEntriesService _dayEntriesService;
        private const int BatchSize = 5;

        public List<DayEntryDto> DayEntriesList { get; set; } = new List<DayEntryDto>();

        public MessageProcessorService(IDayEntriesService dayEntriesService)
        {
            _dayEntriesService = dayEntriesService;
        }

        public async Task ProcessAsync(ConsumeResult<Null, string> response)
        {
            if (response.IsPartitionEOF && DayEntriesList.Count == 0)
                return;

            if (response.IsPartitionEOF && DayEntriesList.Count > 0)
            {
                await _dayEntriesService.InsertDayEntriesAsync(DayEntriesList);

                DayEntriesList.Clear();

                return;
            }

            if (DayEntriesList.Count == BatchSize)
            {
                await _dayEntriesService.InsertDayEntriesAsync(DayEntriesList);

                DayEntriesList.Clear();
            }

            var dayEntry =
                    JsonSerializer.Deserialize<DayEntryDto>(
                        response.Message.Value,
                        SerializerConfiguration.DefaultSerializerOptions)
                    ?? throw new ArgumentException("Were not able to deserialize day entries");

            DayEntriesList.Add(dayEntry);
        }
    }
}
