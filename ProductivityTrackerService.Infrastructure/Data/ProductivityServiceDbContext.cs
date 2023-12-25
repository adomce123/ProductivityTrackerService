using Microsoft.EntityFrameworkCore;
using ProductivityTrackerService.Core.Entities;

namespace ProductivityTrackerService.Infrastructure.Data
{
    public class ProductivityServiceDbContext : DbContext
    {
        public ProductivityServiceDbContext(DbContextOptions<ProductivityServiceDbContext> options) 
            : base(options) { }

        public DbSet<DayEntryEntity> DayEntries => Set<DayEntryEntity>();
    }
}
