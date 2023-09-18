using ProductivityTrackerService.Data;

namespace ProductivityTrackerService.Repositories
{
    public interface IDayEntriesRepository
    {
        Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync();
        Task InsertDayEntriesAsync(IEnumerable<DayEntryEntity> dayEntries);
    }
}
