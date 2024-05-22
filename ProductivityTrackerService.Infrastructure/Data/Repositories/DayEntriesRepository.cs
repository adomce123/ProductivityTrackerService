using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService.Core.Entities;
using ProductivityTrackerService.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductivityTrackerService.Infrastructure.Data.Repositories
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
            return await _dbContext.DayEntries.OrderBy(entry => entry.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task InsertDayEntriesAsync(
            IEnumerable<DayEntryEntity> dayEntries, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await _dbContext.DayEntries.AddRangeAsync(dayEntries, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
