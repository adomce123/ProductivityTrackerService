using AutoFixture;
using FluentAssertions;
using Moq;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Extensions;
using ProductivityTrackerService.Core.Interfaces;
using ProductivityTrackerService.Core.Services;
using Xunit;

namespace ProductivityTrackerService.Tests
{
    public class DayEntriesServiceShould
    {
        private readonly Fixture _fixture;
        private readonly Mock<IDayEntriesRepository> _dayEntriesRepositoryMock;
        private readonly DayEntriesService _dayEntriesService;

        public DayEntriesServiceShould()
        {
            _fixture = new Fixture();

            _dayEntriesRepositoryMock = new Mock<IDayEntriesRepository>();

            _dayEntriesService = new DayEntriesService(_dayEntriesRepositoryMock.Object);
        }

        [Fact]
        public async Task ReturnDayEntries()
        {
            //ARRANGE
            var dayEntryEntities = _fixture.Create<IEnumerable<DayEntryEntity>>();

            _dayEntriesRepositoryMock
                .Setup(_ => _.GetDayEntriesAsync())
                .ReturnsAsync(dayEntryEntities);

            //ACT
            var result = await _dayEntriesService.GetDayEntriesAsync();

            //ASSERT
            result.Should().BeEquivalentTo(dayEntryEntities);

            _dayEntriesRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task InsertDayEntries()
        {
            //ARRANGE
            var dayEntryDtos = _fixture.Create<IEnumerable<DayEntryDto>>();

            _dayEntriesRepositoryMock
                .Setup(_ => _.InsertDayEntriesAsync(It.IsAny<IEnumerable<DayEntryEntity>>()));

            //ACT
            await _dayEntriesService.InsertDayEntriesAsync(dayEntryDtos);

            //ASSERT
            _dayEntriesRepositoryMock.VerifyAll();
        }

        [Fact]
        public void ConvertDtosToEntities()
        {
            //ARRANGE
            var dayEntryDtos = _fixture.Create<IEnumerable<DayEntryDto>>();

            var expectedEntities = new List<DayEntryEntity>();

            foreach (var dayEntry in dayEntryDtos)
            {
                expectedEntities.Add(new DayEntryEntity
                {
                    Id = dayEntry.Id,
                    AddedTimestamp = DateTime.UtcNow,
                    Date = dayEntry.Date,
                    WeekDay = dayEntry.WeekDay,
                    WakeUpTime = dayEntry.WakeUpTime,
                    ScreenTime = dayEntry.ScreenTime,
                    ProjectWork = dayEntry.ProjectWork,
                    WentToGym = dayEntry.WentToGym,
                    Score = dayEntry.Score,
                    Description = dayEntry.Description
                });
            }

            //ACT
            var actualEntities = dayEntryDtos.ToEntities();

            //ASSERT
            actualEntities.Should().BeEquivalentTo(expectedEntities, option => option
                .Excluding(entity => entity.AddedTimestamp));
        }
    }
}