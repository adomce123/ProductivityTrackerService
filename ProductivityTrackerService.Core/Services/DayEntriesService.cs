using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Extensions;
using ProductivityTrackerService.Core.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Services
{
    public class DayEntriesService(IDayEntriesRepository _dayEntriesRepository) : IDayEntriesService
    {
        public async Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync()
        {
            return await _dayEntriesRepository.GetDayEntriesAsync();
        }

        public async Task InsertDayEntriesAsync(
            IEnumerable<DayEntryDto> dayEntryDtos, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var dayEntryEntities = dayEntryDtos.ToEntities();

            await _dayEntriesRepository.InsertDayEntriesAsync(dayEntryEntities, ct);
        }
    }
}
