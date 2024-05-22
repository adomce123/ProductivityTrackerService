using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ProductivityTrackerService.Application.Serialization;
using ProductivityTrackerService.Core.DTOs;
using ProductivityTrackerService.Core.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ProductivityTrackerService.Application.Services
{
    public class MessageProcessorService : IMessageProcessorService
    {
        private const int BatchSize = 5;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentQueue<DayEntryDto> _dayEntriesQueue = new ConcurrentQueue<DayEntryDto>();
        private readonly ILogger<MessageProcessorService> _logger;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly AsyncRetryPolicy _retryPolicy;

        public MessageProcessorService(
            IServiceScopeFactory scopeFactory,
            ILogger<MessageProcessorService> logger,
            IKafkaProducer kafkaProducer)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _kafkaProducer = kafkaProducer;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, $"Attempt {retryCount} failed. Retrying in {timeSpan}.");
                });
        }

        public async Task ProcessAsync(ConsumeResult<Null, string> response, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (response.IsPartitionEOF)
            {
                if (_dayEntriesQueue.Count > 0)
                    FireAndForgetInsertDayEntriesBatch(ct);

                return;
            }
            else if (_dayEntriesQueue.Count >= BatchSize)
            {
                FireAndForgetInsertDayEntriesBatch(ct);
            }

            var dayEntry = JsonSerializer.Deserialize<DayEntryDto>(
                response.Message.Value,
                SerializerConfiguration.DefaultSerializerOptions)
                ?? throw new ArgumentException("Could not deserialize day entry");

            _dayEntriesQueue.Enqueue(dayEntry);
        }

        public async Task HandleNotProcessedMessages(List<DayEntryDto>? batch = null)
        {
            if (batch == null)
                batch = new List<DayEntryDto>();

            if (_dayEntriesQueue.Count > 0)
            {
                while (_dayEntriesQueue.TryDequeue(out var dayEntry))
                {
                    batch.Add(dayEntry);
                }
            }

            foreach (var msg in batch)
                _logger.LogError($"Sending to error topic with id {msg.Id}");

            var errorMessage = JsonSerializer.Serialize(batch);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _kafkaProducer.ProduceAsync("error_topic", errorMessage);
            });
        }

        private void FireAndForgetInsertDayEntriesBatch(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _ = InsertDayEntriesBatchInternalAsync(ct);
        }

        private async Task InsertDayEntriesBatchInternalAsync(CancellationToken ct)
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
                try
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        _logger.LogInformation("Trying to save batch to database..");
                        throw new InvalidOperationException("OH NOOOO!!!");

                        await using var serviceScope = _scopeFactory.CreateAsyncScope();
                        var scopedDayEntriesService = serviceScope.ServiceProvider.GetRequiredService<IDayEntriesService>();

                        ct.ThrowIfCancellationRequested();
                        await scopedDayEntriesService.InsertDayEntriesAsync(batch, ct);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to insert day entries batch after retries. Sending to error topic.");
                    await HandleNotProcessedMessages(batch);
                }
            }
        }
    }
}
