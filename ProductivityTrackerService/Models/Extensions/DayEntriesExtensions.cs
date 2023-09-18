using ProductivityTrackerService.Data;

namespace ProductivityTrackerService.Models.Extensions
{
    public static class DayEntriesExtensions
    {
        public static IEnumerable<DayEntryEntity> ToEntities(this IEnumerable<DayEntryDto> dayEntryDtos)
        {
            var dayEntryEntities = new List<DayEntryEntity>();
            
            foreach (var dayEntryDto in dayEntryDtos)
            {
                var dayEntryEntity = new DayEntryEntity
                {
                    Id = dayEntryDto.Id,
                    AddedTimestamp = DateTime.UtcNow,
                    Date = dayEntryDto.Date,
                    WeekDay = dayEntryDto.WeekDay,
                    WakeUpTime = dayEntryDto.WakeUpTime,
                    ScreenTime = dayEntryDto.ScreenTime,
                    ProjectWork = dayEntryDto.ProjectWork,
                    WentToGym = dayEntryDto.WentToGym,
                    Score = dayEntryDto.Score,
                    Description = dayEntryDto.Description
                };
                dayEntryEntities.Add(dayEntryEntity);
            }

            return dayEntryEntities;
        }
    }
}
