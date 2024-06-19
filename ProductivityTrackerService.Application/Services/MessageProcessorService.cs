using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ProductivityTrackerService.Application.Serialization;
using ProductivityTrackerService.Core.DTOs;
using ProductivityTrackerService.Core.Interfaces;
using System.Text.Json;

namespace ProductivityTrackerService.Application.Services
{
    public class MessageProcessorService : IMessageProcessorService
    {
        private const int BatchSize = 5;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IQueue<DayEntryDto> _dayEntriesQueue;
        private readonly ISharedCache _sharedCache;
        private readonly ILogger<MessageProcessorService> _logger;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly AsyncRetryPolicy _retryPolicy;

        public MessageProcessorService(
            IServiceScopeFactory scopeFactory,
            ILogger<MessageProcessorService> logger,
            IKafkaProducer kafkaProducer,
            IQueue<DayEntryDto> dayEntriesQueue,
            ISharedCache sharedCache)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _dayEntriesQueue = dayEntriesQueue;
            _sharedCache = sharedCache;
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, $"Attempt {retryCount} failed. Retrying in {timeSpan}.");
                });
        }

        public async Task ProcessAsync(ConsumeResult<int, string> response, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (_dayEntriesQueue.Count >= BatchSize)
            {
                var cacheResult = await _sharedCache.GetAsync<DayEntryDto>("someKey");
                _logger.LogInformation($"Reading from cache: {cacheResult}");

                FireAndForgetInsertDayEntriesBatch(ct);
            }

            if (response.IsPartitionEOF)
                return;

            DayEntryDto dayEntryDto;
            try
            {
                dayEntryDto = JsonSerializer.Deserialize<DayEntryDto>(
                    response.Message.Value,
                    SerializerConfiguration.DefaultSerializerOptions)
                    ?? throw new ArgumentException("Could not deserialize day entry");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Provided message could not be parsed" +
                    $" to day entry: {response.Message.Value}, with exception {ex.Message}");
            }

            await _sharedCache.SetAsync("someKey", dayEntryDto);
            _logger.LogInformation("Writing to cache");

            _dayEntriesQueue.Enqueue(dayEntryDto);

            await Task.CompletedTask;
        }

        public async Task HandleNotProcessedMessages(List<DayEntryDto>? batch = null)
        {
            if (batch == null)
                batch = new List<DayEntryDto>();

            if (_dayEntriesQueue.Count > 0)
            {
                while (_dayEntriesQueue.TryDequeue(out var dayEntry))
                {
                    if (dayEntry != null)
                        batch.Add(dayEntry);
                }
            }

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _kafkaProducer.ProduceAsync(batch);
            });
        }

        private void FireAndForgetInsertDayEntriesBatch(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _ = InsertDayEntriesBatchInternalAsync(ct);
        }

        public async Task InsertDayEntriesBatchInternalAsync(CancellationToken ct)
        {
            List<DayEntryDto> batch = new List<DayEntryDto>();

            while (_dayEntriesQueue.TryDequeue(out var dayEntry))
            {
                if (dayEntry != null)
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
