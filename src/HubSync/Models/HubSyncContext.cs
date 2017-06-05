using Microsoft.EntityFrameworkCore;

namespace HubSync.Models
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

            modelBuilder.Entity<SyncHistory>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<SyncHistory>()
                .HasOne(s => s.Repository)
                .WithMany(r => r.HistoryEntries);

            modelBuilder.Entity<Repository>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Repository>()
                .HasAlternateKey(r => new { r.Owner, r.Name });

            modelBuilder.Entity<IssueLabel>()
                .HasKey(il => new { il.IssueId, il.LabelId });
            modelBuilder.Entity<IssueLabel>()
                .HasOne(il => il.Issue)
                .WithMany(i => i.Labels);
            modelBuilder.Entity<IssueLabel>()
                .HasOne(il => il.Label)
                .WithMany(l => l.Issues);

            modelBuilder.Entity<IssueAssignee>()
                .HasKey(ia => new { ia.IssueId, ia.UserId });
            modelBuilder.Entity<IssueAssignee>()
                .HasOne(ia => ia.Issue)
                .WithMany(i => i.Assignees);
            modelBuilder.Entity<IssueAssignee>()
                .HasOne(ia => ia.User)
                .WithMany(u => u.AssignedIssues);

            modelBuilder.Entity<Issue>()
                .HasKey(i => i.Id);
            modelBuilder.Entity<Issue>()
                .HasAlternateKey(i => new { i.Number, i.RepositoryId });
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Repository)
                .WithMany(r => r.Issues);
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Milestone)
                .WithMany(m => m.Issues);
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.User)
                .WithMany(u => u.CreatedIssues);
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.ClosedBy)
                .WithMany(u => u.ClosedIssues);
        }
    }
}
