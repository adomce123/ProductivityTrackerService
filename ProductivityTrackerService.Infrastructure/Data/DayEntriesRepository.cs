using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Data
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
