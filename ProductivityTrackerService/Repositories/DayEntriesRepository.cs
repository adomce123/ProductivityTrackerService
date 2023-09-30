using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService.Data;

namespace ProductivityTrackerService.Repositories
{
    public class DayEntriesRepository : IDayEntriesRepository
    {
        private readonly IDbContextFactory<ProductivityServiceDbContext> _dbContextFactory;

        public DayEntriesRepository(IDbContextFactory<ProductivityServiceDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IEnumerable<DayEntryEntity>> GetDayEntriesAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.DayEntries.OrderBy(entry => entry.Id).ToListAsync();
        }

        public async Task InsertDayEntriesAsync(IEnumerable<DayEntryEntity> dayEntries)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await context.BulkInsertAsync(dayEntries);
        }
    }
}
