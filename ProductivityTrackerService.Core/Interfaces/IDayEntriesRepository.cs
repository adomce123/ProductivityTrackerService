using ProductivityTrackerService.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface IDayEntriesRepository
    {
        Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync();
        Task InsertDayEntriesAsync(
            IEnumerable<DayEntryEntity> dayEntries, CancellationToken ct);
    }
}
