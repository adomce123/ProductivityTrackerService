using ProductivityTrackerService.Data;
using ProductivityTrackerService.Models;

namespace ProductivityTrackerService.Services
{
    public interface IDayEntriesService
    {
        Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync();
        Task InsertDayEntriesAsync(IEnumerable<DayEntryDto> dayEntryDtos);
    }
}
