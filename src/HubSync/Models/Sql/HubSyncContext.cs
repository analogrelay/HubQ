using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HubSync.Models.Sql
{
    public class HubSyncContext : DbContext
    {
        public DbSet<SyncHistory> SyncHistory { get; set; }
        public DbSet<Repository> Repositories { get; set; }
        public DbSet<Issue> Issues { get; set; }
        public DbSet<Milestone> Milestones { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<IssueLabel> IssueLabels { get; set; }
        public DbSet<IssueAssignee> IssueAssignees { get; set; }

        public HubSyncContext() : base(new DbContextOptionsBuilder<HubSyncContext>()
            .UseSqlServer("Server = (localdb)\\mssqllocaldb; Database=HubSync;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options)
        {
        }

        public HubSyncContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Issue>()
                .HasKey(i => i.Id);
            modelBuilder.Entity<Issue>()
                .HasAlternateKey(i => new { i.Number, i.RepositoryId });
            modelBuilder.Entity<Issue>()
                .HasAlternateKey(i => i.GitHubId);
            modelBuilder.Entity<Issue>()
                .HasMany(i => i.Assignees)
                .WithOne(ia => ia.Issue)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Issue>()
                .HasMany(i => i.Labels)
                .WithOne(il => il.Issue)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Issue>()
                .OwnsOne(i => i.PullRequest);
            modelBuilder.Entity<Issue>()
                .OwnsOne(i => i.Reactions);

            modelBuilder.Entity<IssueAssignee>()
                .HasKey(ia => new { ia.IssueId, ia.UserId });

            modelBuilder.Entity<IssueLabel>()
                .HasKey(il => new { il.IssueId, il.LabelId });

            modelBuilder.Entity<Label>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Label>()
                .HasAlternateKey(l => new { l.RepositoryId, l.Name });
            modelBuilder.Entity<Label>()
                .HasMany(l => l.Issues)
                .WithOne(il => il.Label)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Milestone>()
                .HasKey(m => m.Id);
            modelBuilder.Entity<Milestone>()
                .HasAlternateKey(m => new { m.RepositoryId, m.Number });
            modelBuilder.Entity<Milestone>()
                .HasMany(m => m.Issues)
                .WithOne(i => i.Milestone)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Repository>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Repository>()
                .HasAlternateKey(r => r.GitHubId);
            modelBuilder.Entity<Repository>()
                .HasAlternateKey(r => new { r.Owner, r.Name });
            modelBuilder.Entity<Repository>()
                .HasMany(r => r.Issues)
                .WithOne(i => i.Repository)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Repository>()
                .HasMany(r => r.Labels)
                .WithOne(l => l.Repository)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Repository>()
                .HasMany(r => r.HistoryEntries)
                .WithOne(s => s.Repository)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SyncHistory>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<User>()
                .HasAlternateKey(u => u.GitHubId);
            modelBuilder.Entity<User>()
                .HasAlternateKey(u => u.Login);
            modelBuilder.Entity<User>()
                .HasMany(u => u.ClosedIssues)
                .WithOne(i => i.ClosedBy)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>()
                .HasMany(u => u.CreatedIssues)
                .WithOne(i => i.User)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>()
                .HasMany(u => u.AssignedIssues)
                .WithOne(ia => ia.User)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
