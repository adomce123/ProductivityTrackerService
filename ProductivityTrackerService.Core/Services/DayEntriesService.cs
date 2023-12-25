using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Extensions;
using ProductivityTrackerService.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Services
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
