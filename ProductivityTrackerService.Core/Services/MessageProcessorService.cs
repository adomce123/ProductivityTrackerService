using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductivityTrackerService.Core.Configuration;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService
{
    public class MessageProcessorService : IMessageProcessor
    {
        private const int BatchSize = 5;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentQueue<DayEntryDto> _dayEntriesQueue = new ConcurrentQueue<DayEntryDto>();

        public MessageProcessorService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task ProcessAsync(ConsumeResult<Null, string> response, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (response.IsPartitionEOF && _dayEntriesQueue.Count == 0)
                return Task.CompletedTask;

            if (response.IsPartitionEOF && _dayEntriesQueue.Count > 0)
            {
                FireAndForgetInsertDayEntriesBatch(ct);
                return Task.CompletedTask;
            }

            if (_dayEntriesQueue.Count >= BatchSize)
            {
                FireAndForgetInsertDayEntriesBatch(ct);
            }

            var dayEntry = JsonSerializer.Deserialize<DayEntryDto>(
                response.Message.Value,
                SerializerConfiguration.DefaultSerializerOptions)
                ?? throw new ArgumentException("Could not deserialize day entry");

            _dayEntriesQueue.Enqueue(dayEntry);

            return Task.CompletedTask;
        }

        private void FireAndForgetInsertDayEntriesBatch(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _ = InsertDayEntriesBatchInternalAsync(ct);
        }

        private async Task InsertDayEntriesBatchInternalAsync(CancellationToken ct)
        {
            try
            {
                List<DayEntryDto> batch = new List<DayEntryDto>();

                while (_dayEntriesQueue.TryDequeue(out var dayEntry))
                {
                    batch.Add(dayEntry);

                    if (batch.Count >= BatchSize)
                    {
                        break;
                    }
                }

                if (batch.Count > 0)
                {
                    await using var serviceScope = _scopeFactory.CreateAsyncScope();
                    var scopedDayEntriesService = serviceScope.ServiceProvider.GetRequiredService<IDayEntriesService>();

                    ct.ThrowIfCancellationRequested();
                    await scopedDayEntriesService.InsertDayEntriesAsync(batch, ct);
                }
            }
            catch (Exception ex)
            {
                var logger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<MessageProcessorService>>();
                logger.LogError(ex, "Error occurred while inserting day entries batch");
            }
        }
    }
}
