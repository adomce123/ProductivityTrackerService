using ProductivityTrackerService.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IDayEntriesService
    {
        Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync();
        Task InsertDayEntriesAsync(IEnumerable<DayEntryDto> dayEntryDtos);
    }
}
