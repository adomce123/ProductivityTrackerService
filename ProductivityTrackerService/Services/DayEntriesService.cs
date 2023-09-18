using ProductivityTrackerService.Data;
using ProductivityTrackerService.Models;
using ProductivityTrackerService.Models.Extensions;
using ProductivityTrackerService.Repositories;

namespace ProductivityTrackerService.Services
{
    public class DayEntriesService : IDayEntriesService
    {
        private readonly IDayEntriesRepository _dayEntriesRepository;

        public DayEntriesService(IDayEntriesRepository dayEntriesRepository)
        {
            _dayEntriesRepository = dayEntriesRepository;
        }

        public async Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync()
        {
            return await _dayEntriesRepository.GetDayEntriesAsync();
        }

        public async Task InsertDayEntriesAsync(IEnumerable<DayEntryDto> dayEntryDtos)
        {
            var dayEntryEntities = dayEntryDtos.ToEntities();

            await _dayEntriesRepository.InsertDayEntriesAsync(dayEntryEntities);
        }
    }
}
