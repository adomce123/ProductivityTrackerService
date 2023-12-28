using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Core.Services;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class MessageConsumerServiceShould
    {
        private readonly Mock<ILogger<MessageConsumerService>> _loggerMock;
        private readonly Mock<IMessageProcessor> _messageProcessorMock;
        private readonly Mock<IKafkaConsumer> _kafkaConsumerMock;
        private readonly MessageConsumerService _messageConsumerService;

        public MessageConsumerServiceShould()
        {
            _loggerMock = new Mock<ILogger<MessageConsumerService>>();
            _messageProcessorMock = new Mock<IMessageProcessor>();
            _kafkaConsumerMock = new Mock<IKafkaConsumer>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IKafkaConsumer)))
                .Returns(_kafkaConsumerMock.Object);

            serviceProvider
                .Setup(x => x.GetService(typeof(IMessageProcessor)))
                .Returns(_messageProcessorMock.Object);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(serviceScope.Object);

            _messageConsumerService = new MessageConsumerService(
                serviceScopeFactory.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ConsumeMessageAndSendForProcessingAndStoreOffset()
        {
            //ARRANGE
            var cancellationTokenSource = new CancellationTokenSource();

            var consumeResult = new ConsumeResult<Null, string>()
            {
                Message = new Message<Null, string>()
            };

            _kafkaConsumerMock.Setup(_ => _.ConsumeMessageAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(_ => cancellationTokenSource.Cancel())
                .ReturnsAsync(consumeResult);

            _messageProcessorMock.Setup(_ => _.ProcessAsync(consumeResult));

            //ACT
            await _messageConsumerService.StartAsync(cancellationTokenSource.Token);

            //ASSERT
            _kafkaConsumerMock
                .Verify(service => service.ConsumeMessageAsync(It.IsAny<CancellationToken>()),
                Times.Once());

            _messageProcessorMock
                .Verify(service => service.ProcessAsync(consumeResult), Times.Once());

            _kafkaConsumerMock
                .Verify(service => service.StoreMessageOffset(consumeResult),
                Times.Once());
        }

        [Fact]
        public async Task LogErrorAndStoreOffsetIfProcessingFailed()
        {
            //ARRANGE
            var cancellationTokenSource = new CancellationTokenSource();

            var consumeResult = new ConsumeResult<Null, string>()
            {
                Message = new Message<Null, string>()
            };

            _kafkaConsumerMock.Setup(_ => _.ConsumeMessageAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(_ => cancellationTokenSource.Cancel())
                .ReturnsAsync(consumeResult);

            _messageProcessorMock
                .Setup(_ => _.ProcessAsync(consumeResult))
                .ThrowsAsync(new Exception("Processing failed"));

            //ACT
            await _messageConsumerService.StartAsync(cancellationTokenSource.Token);

            //ASSERT
            var exceptionThrown = await Assert.ThrowsAsync<Exception>
                (async () => await _messageProcessorMock.Object.ProcessAsync(consumeResult));

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()
                        .Contains("Message processing failed with exception")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

            Assert.Equal("Processing failed", exceptionThrown.Message);

            _kafkaConsumerMock
                .Verify(service => service.StoreMessageOffset(consumeResult),
                Times.Once());
        }
    }
}
