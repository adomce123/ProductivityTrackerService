using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Messaging;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class BaseConsumerShould
    {
        private class TestBaseConsumer : BaseConsumer
        {
            public TestBaseConsumer(
                IMessageProcessorService messageProcessor,
                ILogger<BaseConsumer> logger,
                IConsumer<int, string> consumer,
                string topic)
                : base(messageProcessor, logger, topic, consumer)
            {
            }

            // Expose the protected ExecuteAsync method for testing
            public Task TestExecuteAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
        }

        private readonly Mock<IMessageProcessorService> _mockMessageProcessor;
        private readonly Mock<ILogger<BaseConsumer>> _mockLogger;
        private readonly Mock<IConsumer<int, string>> _mockConsumer;

        public BaseConsumerShould()
        {
            _mockMessageProcessor = new Mock<IMessageProcessorService>();
            _mockLogger = new Mock<ILogger<BaseConsumer>>();
            _mockConsumer = new Mock<IConsumer<int, string>>();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldProcessMessage()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var testTopic = "test-topic";
            var testConsumer = new TestBaseConsumer(
                _mockMessageProcessor.Object,
                _mockLogger.Object,
                _mockConsumer.Object,
                testTopic
            );

            var consumeResult = new ConsumeResult<int, string>
            {
                Message = new Message<int, string> { Key = 1, Value = "Test message" }
            };

            _mockConsumer
                .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
                .Returns(consumeResult);

            _mockMessageProcessor.Setup(mp => mp.ProcessAsync(
                It.IsAny<ConsumeResult<int, string>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var executeTask = testConsumer.TestExecuteAsync(cancellationTokenSource.Token);
            await Task.Delay(1000); // Allow some time for the consumer loop to run
            cancellationTokenSource.Cancel(); // Cancel the token to stop the consumer loop
            await executeTask; // Wait for the consumer loop to exit

            // Assert
            _mockConsumer
                .Verify(c => c.Consume(It.IsAny<CancellationToken>()), Times.AtLeastOnce);

            _mockMessageProcessor.Verify(mp => mp.ProcessAsync(
                It.IsAny<ConsumeResult<int, string>>(),
                It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleOperationCanceledException()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var testTopic = "test-topic";
            var testConsumer = new TestBaseConsumer(
                _mockMessageProcessor.Object,
                _mockLogger.Object,
                _mockConsumer.Object,
                testTopic
            );

            _mockConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>()))
                         .Throws(new OperationCanceledException());

            // Act
            var executeTask = testConsumer.TestExecuteAsync(cancellationTokenSource.Token);
            await Task.Delay(1000); // Allow some time for the consumer loop to run
            cancellationTokenSource.Cancel(); // Cancel the token to stop the consumer loop
            await executeTask; // Wait for the consumer loop to exit

            // Assert
            _mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(level => level == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("operation was cancelled")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
        }
    }
}