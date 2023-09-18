using AutoFixture;
using Confluent.Kafka;
using Moq;
using ProductivityTrackerService.Data;
using ProductivityTrackerService.Repositories;
using ProductivityTrackerService.Services;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class MessageProcessorShould
    {
        private readonly Fixture _fixture;
        private readonly Mock<IDayEntriesRepository> _dayEntriesRepositoryMock;
        private readonly Mock<IDayEntriesService> _dayEntriesServiceMock;
        private readonly MessageProcessor _messageProcessor;

        public MessageProcessorShould()
        {
            _fixture = new Fixture();

            _dayEntriesRepositoryMock = new Mock<IDayEntriesRepository>();

            _dayEntriesServiceMock = new Mock<IDayEntriesService>();

            _messageProcessor = new MessageProcessor(_dayEntriesServiceMock.Object);
        }

        [Fact]
        public async Task NotDoAnythingIfPartitionEofAndMessageListIsEmpty()
        {
            //ARRANGE
            var message = new ConsumeResult<Null, string>()
            {
                IsPartitionEOF = true
            };


            var dayEntryEntities = _fixture.Create<IEnumerable<DayEntryEntity>>();

            await _messageProcessor.ProcessAsync(message);

            _dayEntriesRepositoryMock
                .Verify(repository => repository
                .InsertDayEntriesAsync(dayEntryEntities), Times.Never());
        }

    }
}
