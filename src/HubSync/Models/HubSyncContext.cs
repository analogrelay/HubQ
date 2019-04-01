using Microsoft.EntityFrameworkCore;

namespace HubSync.Models
{
    public class HubSyncContext : DbContext
    {
        // We trust that EF has initialized this. Initializing to `null!` tells C# nullability analyzer to GTFO
        public DbSet<SyncLogEntry> SyncLog { get; set; } = null!;
        public DbSet<Repository> Repositories { get; set; } = null!;
        public DbSet<Issue> Issues { get; set; } = null!;
        public DbSet<Actor> Actors { get; set; } = null!;

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

                syncLog.Property(e => e.RepositoryId).IsRequired();
                syncLog.Property(e => e.User).IsRequired();

                syncLog
                    .HasOne(e => e.Repository)
                    .WithMany(r => r!.LogEntries)
                    .HasForeignKey(e => e.RepositoryId);

                syncLog.ToTable("SyncLog");
            });

            modelBuilder.Entity<Actor>(actor =>
            {
                actor.HasIndex(a => a.GitHubId).IsUnique();
                actor.Property(a => a.Login).IsRequired();
            });

            modelBuilder.Entity<Repository>(repo =>
            {
                repo.HasIndex(r => r.GitHubId).IsUnique();
                repo.Property(r => r.Owner).IsRequired();
                repo.Property(r => r.Name).IsRequired();
            });

            modelBuilder.Entity<Issue>(issue =>
            {
                issue.HasIndex(i => i.GitHubId).IsUnique();
                issue
                    .HasOne(i => i.Repository)
                    .WithMany(r => r!.Issues)
                    .HasForeignKey(i => i.RepositoryId);

                issue
                    .HasOne(i => i.Author)
                    .WithMany(a => a!.Issues)
                    .HasForeignKey(i => i.AuthorId);
            });
        }
    }
}
