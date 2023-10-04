﻿using AutoFixture;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class MessageConsumerShould
    {
        private readonly Mock<ILogger<MessageConsumer>> _loggerMock;
        private readonly Mock<IMessageProcessor> _messageProcessorMock;
        private readonly Mock<IKafkaConsumer> _kafkaConsumerMock;
        private readonly MessageConsumer _messageConsumer;

        public MessageConsumerShould()
        {   
            _loggerMock = new Mock<ILogger<MessageConsumer>>();
            _messageProcessorMock = new Mock<IMessageProcessor>();
            _kafkaConsumerMock = new Mock<IKafkaConsumer>();

            _messageConsumer = new MessageConsumer(
                _loggerMock.Object, _messageProcessorMock.Object, _kafkaConsumerMock.Object);
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
            await _messageConsumer.StartAsync(cancellationTokenSource.Token);

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
            await _messageConsumer.StartAsync(cancellationTokenSource.Token);

            //ASSERT
            var exceptionThrown = await Assert.ThrowsAsync<Exception>
                (async () => await _messageProcessorMock.Object.ProcessAsync(consumeResult));

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

            Assert.Equal("Processing failed", exceptionThrown.Message);

            _kafkaConsumerMock
                .Verify(service => service.StoreMessageOffset(consumeResult),
                Times.Once());
        }
    }
}