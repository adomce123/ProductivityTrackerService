using Microsoft.EntityFrameworkCore;

namespace ProductivityTrackerService.Data
{
    public class ProductivityServiceDbContext : DbContext
    {
        public ProductivityServiceDbContext(DbContextOptions<ProductivityServiceDbContext> options) 
            : base(options) { }

        public DbSet<DayEntryEntity> DayEntries => Set<DayEntryEntity>();
    }
}
