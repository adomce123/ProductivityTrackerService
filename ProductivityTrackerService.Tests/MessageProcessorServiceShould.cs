using AutoFixture;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProductivityTrackerService.Application.Services;
using ProductivityTrackerService.Core.DTOs;
using ProductivityTrackerService.Core.Interfaces;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class MessageProcessorServiceShould
    {
        private readonly Fixture _fixture;
        private readonly CancellationToken _ctMock;
        private readonly Mock<IDayEntriesService> _dayEntriesServiceMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IKafkaProducer> _kafkaProducerMock;
        private readonly Mock<IQueue<DayEntryDto>> _queueMock;
        private readonly MessageProcessorService _messageProcessor;

        public MessageProcessorServiceShould()
        {
            _fixture = new Fixture();

            _ctMock = new CancellationToken();

            _dayEntriesServiceMock = new Mock<IDayEntriesService>();

            _serviceScopeMock = new Mock<IServiceScope>();

            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            // Setup IServiceScopeFactory to return IServiceScope
            _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);

            // Setup IServiceScope to return IDayEntriesService
            _serviceScopeMock
                .Setup(x => x.ServiceProvider.GetService(typeof(IDayEntriesService)))
                .Returns(_dayEntriesServiceMock.Object);

            _kafkaProducerMock = new Mock<IKafkaProducer>();

            var loggerMock = new Mock<ILogger<MessageProcessorService>>();

            _queueMock = new Mock<IQueue<DayEntryDto>>();

            _messageProcessor = new MessageProcessorService(
                _serviceScopeFactoryMock.Object,
                loggerMock.Object,
                _kafkaProducerMock.Object,
                _queueMock.Object);
        }

        [Fact]
        public async Task ProcessAsync_ShouldEnqueueDayEntryButNotCallInsert()
        {
            //ARRANGE
            string jsonString = "{\"Id\":0,\"Date\":\"2023-09-15T00:00:00\",\"WeekDay\":null," +
                "\"WakeUpTime\":\"11:30:01\",\"ScreenTime\":\"03:00:00\",\"ProjectWork\":\"01:00:00\"," +
                "\"WentToGym\":false,\"Score\":0,\"Description\":\"149\"}";

            var consumeResult = new ConsumeResult<int, string>
            {
                IsPartitionEOF = false,
                Message = new Message<int, string> { Key = 1, Value = jsonString }
            };

            var dayEntryEntities = _fixture.Create<IEnumerable<DayEntryDto>>();

            //ACT
            await _messageProcessor.ProcessAsync(consumeResult, _ctMock);

            //ASSERT
            _queueMock.Verify(_ => _.Enqueue(It.IsAny<DayEntryDto>()), Times.Once);

            _dayEntriesServiceMock
                .Verify(_ => _
                .InsertDayEntriesAsync(dayEntryEntities, _ctMock), Times.Never());
        }

        [Fact]
        public async Task ProcessAsync_ShouldNotInsertIfMessageIsEof()
        {
            //ARRANGE
            var message = new ConsumeResult<int, string>()
            {
                IsPartitionEOF = true
            };

            var dayEntryEntities = _fixture.Create<IEnumerable<DayEntryDto>>();

            //ACT
            await _messageProcessor.ProcessAsync(message, _ctMock);

            //ASSERT
            _queueMock.Verify(_ => _.Enqueue(It.IsAny<DayEntryDto>()), Times.Never);

            _dayEntriesServiceMock
                .Verify(repository => repository
                .InsertDayEntriesAsync(dayEntryEntities, _ctMock), Times.Never());
        }

        [Fact]
        public async Task ProcessAsync_ThrowExceptionIfCannotDeserialize()
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

            //ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _messageProcessor.ProcessAsync(message, _ctMock));

            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Never());
        }

        [Fact]
        public async Task InsertDayEntriesBatchInternalAsync_ShouldCallInsertDayEntriesAsync()
        {
            //ARRANGE
            var dayEntry = new DayEntryDto { Id = 1, Date = DateTime.Now };
            _queueMock.Setup(q => q.TryDequeue(out dayEntry)).Returns(true);

            //ACT
            await _messageProcessor.InsertDayEntriesBatchInternalAsync(_ctMock);

            //ARRANGE
            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Once());
        }

        [Fact]
        public async Task InsertDayEntriesBatchInternalAsync_ShouldCallKafkaProducer_OnError_WithRetry()
        {
            //ARRANGE
            var dayEntry = new DayEntryDto { Id = 1, Date = DateTime.Now };
            _queueMock.Setup(q => q.TryDequeue(out dayEntry)).Returns(true);

            _dayEntriesServiceMock
                .Setup(d => d.InsertDayEntriesAsync(It.IsAny<List<DayEntryDto>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            //ACT
            await _messageProcessor.InsertDayEntriesBatchInternalAsync(_ctMock);

            //ARRANGE
            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Exactly(2));

            _kafkaProducerMock
                .Verify(_ => _.ProduceAsync(It.IsAny<List<DayEntryDto>>()), Times.Exactly(1));
        }

        [Fact]
        public async Task ProcessAsync_ShouldThrowOperationCanceledException_WhenTokenIsCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel the token

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _messageProcessor.ProcessAsync(It.IsAny<ConsumeResult<int, string>>(), cts.Token));
        }
    }
}
