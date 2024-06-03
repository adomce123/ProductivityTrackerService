namespace ProductivityTrackerService.Infrastructure.Messaging
{
    using Confluent.Kafka;
    using global::ProductivityTrackerService.Core.Interfaces;
    using global::ProductivityTrackerService.Infrastructure.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    namespace ProductivityTrackerService.Infrastructure.Messaging
    {
        public class GenericConsumer : BackgroundService
        {
            private readonly ILogger<GenericConsumer> _logger;
            private readonly KafkaConsumerSettings _consumerSettings;
            private readonly string _consumerName;
            private readonly IMessageProcessorService _messageProcessor;
            private readonly IConsumer<Null, string> _consumer;

            public GenericConsumer(
                string consumerName,
                IMessageProcessorService messageProcessor,
                ILogger<GenericConsumer> logger,
                KafkaConsumerSettings consumerSettings)
            {
                _consumerName = consumerName;
                _messageProcessor = messageProcessor;
                _logger = logger;
                _consumerSettings = consumerSettings;
                _consumer = new ConsumerBuilder<Null, string>(_consumerSettings.ConsumerConfig).Build();
                _consumer.Subscribe(_consumerSettings.Topic);
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                _logger.LogInformation($"{_consumerSettings.ConsumerName} service started.");

                var consumerTask = Task.Run(() => RunConsumerLoop(stoppingToken), stoppingToken);

                await consumerTask;
            }

            public async Task RunConsumerLoop(CancellationToken stoppingToken)
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var response = _consumer.Consume(stoppingToken);

                        if (response != null)
                        {
                            _logger.LogInformation($"{_consumerSettings.ConsumerName} consumed: {response.Message?.Value}");

                            try
                            {
                                await _messageProcessor.ProcessAsync(response, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"{_consumerSettings.ConsumerName} message " +
                                    $"processing failed with exception: {ex.Message}");
                            }
                            finally
                            {
                                if (!response.IsPartitionEOF)
                                {
                                    _consumer.StoreOffset(response);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation($"{_consumerSettings.ConsumerName} operation was cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_consumerSettings.ConsumerName}.{nameof(ExecuteAsync)} threw an exception.");
                }
                finally
                {
                    _consumer.Close();
                    await _messageProcessor.HandleNotProcessedMessages();
                    _logger.LogInformation($"{_consumerSettings.ConsumerName} is stopping.");
                }
            }
        }
    }

}
