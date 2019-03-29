using Microsoft.EntityFrameworkCore;

namespace HubSync.Models
{
    public class HubSyncContext : DbContext
    {
        // We trust that EF has initialized this. Initializing to `null!` tells C# nullability analyzer to GTFO
        public DbSet<SyncLogEntry> SyncLog { get; set; } = null!;

        public HubSyncContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SyncLogEntry>(syncLog =>
            {
                // TODO: Consider indexes as the logs grow.
                // We always want the latest, don't really need the other entries except for monitoring.

                syncLog.Property(e => e.Owner).IsRequired();
                syncLog.Property(e => e.Repository).IsRequired();
                syncLog.Property(e => e.User).IsRequired();
                syncLog.Property(e => e.Started).IsRequired();
                syncLog.Property(e => e.WaterMark).IsRequired();

                syncLog.ToTable("SyncLog");
            });
        }
    }
}
