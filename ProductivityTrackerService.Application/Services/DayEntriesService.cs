using ProductivityTrackerService.Core.DTOs;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Extensions;
using ProductivityTrackerService.Core.Interfaces;

namespace ProductivityTrackerService.Application.Services
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
