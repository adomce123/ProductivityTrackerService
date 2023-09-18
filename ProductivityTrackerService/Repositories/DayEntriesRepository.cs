using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService.Data;

namespace ProductivityTrackerService.Repositories
{
    public class DayEntriesRepository : IDayEntriesRepository
    {
        private readonly ProductivityServiceDbContext _dbContext;

        public DayEntriesRepository(ProductivityServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync()
        {
            return await _dbContext.DayEntries.OrderBy(entry => entry.Id).ToListAsync();
        }

        public async Task InsertDayEntriesAsync(IEnumerable<DayEntryEntity> dayEntries)
        {
            await _dbContext.BulkInsertAsync(dayEntries);
        }
    }
}
