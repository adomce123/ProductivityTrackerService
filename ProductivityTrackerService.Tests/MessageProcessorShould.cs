using AutoFixture;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Interfaces;
using System.Text.Json;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class MessageProcessorShould
    {
        private readonly Fixture _fixture;
        private readonly Mock<IDayEntriesRepository> _dayEntriesRepositoryMock;
        private readonly CancellationToken _ctMock;
        private readonly Mock<IDayEntriesService> _dayEntriesServiceMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly MessageProcessorService _messageProcessor;

        public MessageProcessorShould()
        {
            _fixture = new Fixture();

            _dayEntriesRepositoryMock = new Mock<IDayEntriesRepository>();
            _ctMock = new CancellationToken();

            _dayEntriesServiceMock = new Mock<IDayEntriesService>();

            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            _messageProcessor = new MessageProcessorService(
                _serviceScopeFactoryMock.Object, _dayEntriesServiceMock.Object);
        }

        [Fact]
        public async Task NotInsertIfEofAndListEmpty()
        {
            //ARRANGE
            var message = new ConsumeResult<Null, string>()
            {
                IsPartitionEOF = true
            };

            _messageProcessor.DayEntriesList.Clear();

            var dayEntryEntities = _fixture.Create<IEnumerable<DayEntryEntity>>();

            //ACT
            await _messageProcessor.ProcessAsync(message, _ctMock);

            //ASSERT
            _dayEntriesRepositoryMock
                .Verify(repository => repository
                .InsertDayEntriesAsync(dayEntryEntities, _ctMock), Times.Never());

            Assert.Empty(_messageProcessor.DayEntriesList);
        }

        [Fact]
        public async Task InsertIfEofAndListNotEmpty()
        {
            //ARRANGE
            var message = new ConsumeResult<Null, string>()
            {
                IsPartitionEOF = true
            };

            _messageProcessor.DayEntriesList.Add(new DayEntryDto());

            await _messageProcessor.ProcessAsync(message, _ctMock);

            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Once());

            Assert.Empty(_messageProcessor.DayEntriesList);
        }

        [Fact]
        public async Task InsertIfNotEofAndBatchFull()
        {
            //ARRANGE
            var dayEntryDto = new DayEntryDto
            {
                Id = 3041,
                Date = new DateTime(2023, 9, 15),
                WeekDay = "Friday",
                WakeUpTime = new TimeSpan(),
                ScreenTime = new TimeSpan(),
                ProjectWork = new TimeSpan(),
                WentToGym = false,
                Score = 1,
                Description = "test"
            };

            var message = new ConsumeResult<Null, string>()
            {
                IsPartitionEOF = false,
                Message = new Message<Null, string>
                {
                    Key = null!,
                    Value = JsonSerializer.Serialize(dayEntryDto)
                }
            };

            _messageProcessor.DayEntriesList.AddRange(new List<DayEntryDto>
            {
                new DayEntryDto(),
                new DayEntryDto(),
                new DayEntryDto(),
                new DayEntryDto(),
                new DayEntryDto()
            });

            //ACT
            await _messageProcessor.ProcessAsync(message, _ctMock);

            //ASSERT
            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Once());

            Assert.True(_messageProcessor.DayEntriesList.Count == 1);
        }

        [Fact]
        public async Task ThrowExceptionIfCannotDeserialize()
        {
            //ARRANGE
            var message = new ConsumeResult<Null, string>()
            {
                IsPartitionEOF = false,
                Message = new Message<Null, string>
                {
                    Key = null!,
                    Value = "test"
                }
            };

            _messageProcessor.DayEntriesList.AddRange(new List<DayEntryDto>
            {
                new DayEntryDto(),
                new DayEntryDto(),
                new DayEntryDto(),
                new DayEntryDto(),
                new DayEntryDto()
            });

            await Assert.ThrowsAsync<JsonException>(() => _messageProcessor.ProcessAsync(message, _ctMock));

            _dayEntriesServiceMock
                .Verify(service => service
                .InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryDto>>(), _ctMock), Times.Once());

            Assert.Empty(_messageProcessor.DayEntriesList);
        }
    }
}
