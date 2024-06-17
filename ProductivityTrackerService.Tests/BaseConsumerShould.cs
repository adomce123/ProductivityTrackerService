using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Infrastructure.Configuration;
using ProductivityTrackerService.Infrastructure.Messaging.ProductivityTrackerService.Infrastructure.Messaging;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class BaseConsumerShould
    {
        private readonly Mock<ILogger<BaseConsumer>> _loggerMock;
        private readonly Mock<IMessageProcessorService> _messageProcessorMock;
        private readonly Mock<IConsumer<int, string>> _kafkaConsumerMock;
        private readonly BaseConsumer _baseConsumer;

        public BaseConsumerShould()
        {
            _loggerMock = new Mock<ILogger<BaseConsumer>>();
            _messageProcessorMock = new Mock<IMessageProcessorService>();
            _kafkaConsumerMock = new Mock<IConsumer<int, string>>();

            var consumerSettings = new KafkaConsumerSettings
            {
                ConsumerConfig = new ConsumerConfig
                {
                    GroupId = "test-group",
                    BootstrapServers = "localhost:9092"
                },
                Topic = "test-topic"
            };

            _kafkaConsumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
                .Returns(new ConsumeResult<int, string>
                {
                    Message = new Message<int, string> { Value = "test message" },
                    Partition = new Partition(0)
                });

            _baseConsumer = new BaseConsumer(
                _messageProcessorMock.Object, _loggerMock.Object, consumerSettings);
        }

        [Fact]
        public async Task RunConsumerLoop_Should_ProcessMessage_When_MessageIsConsumed()
        {
            // Arrange
            var stoppingToken = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;

            // Act
            await _baseConsumer.StartAsync(stoppingToken);

            // Assert
            _messageProcessorMock.Verify(
                x => x.ProcessAsync(It.IsAny<ConsumeResult<int, string>>(), stoppingToken),
                Times.Once);
        }

        //[Fact]
        //public async Task LogErrorAndStoreOffsetIfProcessingFailed()
        //{
        //    //ARRANGE
        //    var cancellationTokenSource = new CancellationTokenSource();

        //    var consumeResult = new ConsumeResult<Null, string>()
        //    {
        //        Message = new Message<Null, string>()
        //    };

        //    _kafkaConsumerMock.Setup(_ => _.ConsumeMessageAsync(It.IsAny<CancellationToken>()))
        //        .Callback<CancellationToken>(_ => cancellationTokenSource.Cancel())
        //        .ReturnsAsync(consumeResult);

        //    _messageProcessorMock
        //        .Setup(_ => _.ProcessAsync(consumeResult))
        //        .ThrowsAsync(new Exception("Processing failed"));

        //    //ACT
        //    await _baseConsumer.StartAsync(cancellationTokenSource.Token);

        //    //ASSERT
        //    var exceptionThrown = await Assert.ThrowsAsync<Exception>
        //        (async () => await _messageProcessorMock.Object.ProcessAsync(consumeResult));

        //    _loggerMock.Verify(
        //        l => l.Log(
        //            LogLevel.Error,
        //            It.IsAny<EventId>(),
        //            It.Is<It.IsAnyType>((v, t) => v.ToString()
        //                .Contains("Message processing failed with exception")),
        //            It.IsAny<Exception>(),
        //            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

        //    Assert.Equal("Processing failed", exceptionThrown.Message);

        //    _kafkaConsumerMock
        //        .Verify(service => service.StoreMessageOffset(consumeResult),
        //        Times.Once());
        //}
    }
}
