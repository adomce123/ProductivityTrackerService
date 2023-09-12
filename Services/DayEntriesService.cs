using ProductivityTrackerService.Data;

namespace ProductivityTrackerService.Services
{
    public class DayEntriesService : IDayEntriesService
    {
        public Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync()
        {
            throw new NotImplementedException();
        }

        public Task InsertDayEntriesAsync(IEnumerable<DayEntryEntity> dayEntries)
        {
            throw new NotImplementedException();
        }
    }
}
