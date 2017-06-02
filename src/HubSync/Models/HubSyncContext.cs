using Microsoft.EntityFrameworkCore;

namespace HubSync.Models
{
    public class HubSyncContext : DbContext
    {
        public DbSet<SyncStatus> SyncStatus { get; set; }

        public HubSyncContext(DbContextOptions options) : base(options)
        {
        }
    }
}
