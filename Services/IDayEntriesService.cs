using ProductivityTrackerService.Data;

namespace ProductivityTrackerService.Services
{
    public interface IDayEntriesService
    {
        Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync();
        Task InsertDayEntriesAsync(IEnumerable<DayEntryEntity> dayEntries);
    }
}
