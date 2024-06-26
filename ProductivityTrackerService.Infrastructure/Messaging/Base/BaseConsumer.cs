﻿using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductivityTrackerService.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Messaging.Base
{
    public abstract class BaseConsumer : BackgroundService
    {
        private readonly ILogger<BaseConsumer> _logger;
        private readonly IMessageProcessorService _messageProcessor;
        private readonly IConsumer<int, string> _consumer;

        protected BaseConsumer(
            IMessageProcessorService messageProcessor,
            ILogger<BaseConsumer> logger,
            string? topic,
            IConsumer<int, string> consumer)
        {
            _messageProcessor = messageProcessor;
            _logger = logger;
            _consumer = consumer;
            _consumer.Subscribe(topic);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{GetType().Name} service started.");

            var consumerTask = Task.Run(() => RunConsumerLoop(stoppingToken), stoppingToken);

            await consumerTask;
        }

        private async Task RunConsumerLoop(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var response = _consumer.Consume(stoppingToken);

                    if (response != null)
                    {
                        _logger.LogInformation($"{GetType().Name} consumed from partition: {response.Partition}: {response.Message?.Value}");

                        try
                        {
                            await _messageProcessor.ProcessAsync(response, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{GetType().Name} message processing failed with exception: {ex.Message}");
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
                _logger.LogInformation($"{GetType().Name} operation was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{GetType().Name}.{nameof(ExecuteAsync)} threw an exception.");
            }
            finally
            {
                _consumer.Close();
                await _messageProcessor.HandleNotProcessedMessages();
                _logger.LogInformation($"{GetType().Name} is stopping.");
            }
        }
    }
}
