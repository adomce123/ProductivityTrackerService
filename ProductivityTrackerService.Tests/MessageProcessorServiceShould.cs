using AutoFixture;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProductivityTrackerService.Application.Services;
using ProductivityTrackerService.Core.DTOs;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Interfaces;
using System.Text.Json;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class MessageProcessorServiceShould
    {
        private readonly Fixture _fixture;
        private readonly Mock<IDayEntriesRepository> _dayEntriesRepositoryMock;
        private readonly CancellationToken _ctMock;
        private readonly Mock<IDayEntriesService> _dayEntriesServiceMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IKafkaProducer> _kafkaProducerMock;
        private readonly MessageProcessorService _messageProcessor;

        public MessageProcessorServiceShould()
        {
            _fixture = new Fixture();

            _dayEntriesRepositoryMock = new Mock<IDayEntriesRepository>();
            _ctMock = new CancellationToken();

            _dayEntriesServiceMock = new Mock<IDayEntriesService>();

            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            _kafkaProducerMock = new Mock<IKafkaProducer>();

            var loggerMock = new Mock<ILogger<MessageProcessorService>>();

            _messageProcessor = new MessageProcessorService(
                _serviceScopeFactoryMock.Object, loggerMock.Object, _kafkaProducerMock.Object);
        }

        [Fact]
        public async Task NotInsertIfMessageIsEof()
        {
            //ARRANGE
            var message = new ConsumeResult<int, string>()
            {
                IsPartitionEOF = true
            };

            var dayEntryEntities = _fixture.Create<IEnumerable<DayEntryEntity>>();

            //ACT
            await _messageProcessor.ProcessAsync(message, _ctMock);

            //ASSERT
            _dayEntriesRepositoryMock
                .Verify(repository => repository
                .InsertDayEntriesAsync(dayEntryEntities, _ctMock), Times.Never());
        }

        [Fact]
        public async Task InsertIfEofAndListNotEmpty()
        {
            //ARRANGE
            var message = new ConsumeResult<int, string>()
            {
                IsPartitionEOF = true
            };

            await _messageProcessor.ProcessAsync(message, _ctMock);

            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Once());
        }


        [Fact]
        public async Task ThrowExceptionIfCannotDeserialize()
        {
            //ARRANGE
            var message = new ConsumeResult<int, string>()
            {
                IsPartitionEOF = false,
                Message = new Message<int, string>
                {
                    Key = 0,
                    Value = "test"
                }
            };

            await Assert.ThrowsAsync<JsonException>(() => _messageProcessor.ProcessAsync(message, _ctMock));

            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Never());
        }
    }
}
