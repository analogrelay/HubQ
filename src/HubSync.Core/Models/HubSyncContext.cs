using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Octokit;

namespace HubSync.Models
{
    public class HubSyncContext : DbContext
    {
        public static readonly string LocalConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=HubSync;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        // We trust that EF has initialized this. Initializing to `null!` tells C# nullability analyzer to GTFO
        public DbSet<SyncLogEntry> SyncLog { get; set; } = null!;
        public DbSet<Repository> Repositories { get; set; } = null!;
        public DbSet<Issue> Issues { get; set; } = null!;
        public DbSet<Actor> Actors { get; set; } = null!;
        public DbSet<Label> Labels { get; set; } = null!;
        public DbSet<Milestone> Milestones { get; set; } = null!;
        public DbSet<IssueAssignee> IssueAssignees { get; set; } = null!;
        public DbSet<IssueLabel> IssueLabels { get; set; } = null!;
        public DbSet<IssueLink> IssueLinks { get; set; } = null!;
        public DbSet<PullRequest> PullRequests { get; set; } = null!;
        public DbSet<ReviewRequest> ReviewRequests { get; set; } = null!;
        public DbSet<Team> Teams { get; set; } = null!;

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
                    .HasForeignKey(e => e.RepositoryId)
                    .IsRequired();

                syncLog.ToTable("SyncLog");
            });

            modelBuilder.Entity<Actor>(actor =>
            {
                actor.HasIndex(a => a.GitHubId).IsUnique();
                actor.Property(a => a.Name).IsRequired();

                actor.Property(a => a.Kind).HasConversion(new StringToEnumConverter<ActorKind>());

                actor
                    .HasDiscriminator(a => a.Kind)
                    .HasValue<Actor>(ActorKind.Bot)
                    .HasValue<Actor>(ActorKind.User)
                    .HasValue<Actor>(ActorKind.Organization)
                    .HasValue<Team>(ActorKind.Team);
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
                    .HasForeignKey(i => i.RepositoryId)
                    .IsRequired();

                issue
                    .HasOne(i => i.Author)
                    .WithMany(a => a!.Issues)
                    .HasForeignKey(i => i.AuthorId)
                    .IsRequired();

                issue.Property(i => i.Body).IsRequired();
                issue.Property(i => i.Title).IsRequired();
                issue.Property(i => i.State).HasConversion(new EnumToStringConverter<ItemState>());

                issue
                    .HasOne(i => i.Milestone)
                    .WithMany(m => m!.Issues)
                    .HasForeignKey(i => i.MilestoneId)
                    .IsRequired(required: false);

                issue
                    .OwnsOne(i => i.Reactions)
                    .WithOwner();

                issue
                    .HasDiscriminator(i => i.IsPullRequest)
                    .HasValue<Issue>(false)
                    .HasValue<PullRequest>(true);
            });

            modelBuilder.Entity<PullRequest>(pullRequest =>
            {
                pullRequest
                    .OwnsOne(p => p.Head)
                    .WithOwner();

                pullRequest
                    .OwnsOne(p => p.Base)
                    .WithOwner();
            });

            modelBuilder.Entity<ReviewRequest>(reviewer =>
            {
                reviewer
                    .HasOne(r => r.PullRequest)
                    .WithMany(p => p.ReviewRequests)
                    .HasForeignKey(r => r.PullRequestId);

                reviewer
                    .HasOne(r => r.User)
                    .WithMany(u => u.ReviewRequests)
                    .HasForeignKey(r => r.UserId);
            });

            modelBuilder.Entity<IssueAssignee>(issueAssignee =>
            {
                issueAssignee.HasKey(i => new { i.IssueId, i.AssigneeId });

                issueAssignee
                    .HasOne(i => i.Issue)
                    .WithMany(i => i!.Assignees)
                    .HasForeignKey(i => i.IssueId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                issueAssignee
                    .HasOne(i => i.Assignee)
                    .WithMany(a => a!.IssueAssignments)
                    .HasForeignKey(i => i.AssigneeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

            modelBuilder.Entity<Label>(label =>
            {
                label.Property(l => l.RepositoryId).IsRequired();
                label.Property(l => l.Name).IsRequired();

                label
                    .HasOne(l => l.Repository)
                    .WithMany(r => r!.Labels)
                    .HasForeignKey(l => l.RepositoryId)
                    .IsRequired();

                label.ToTable("Labels");
            });

            modelBuilder.Entity<Milestone>(milestone =>
            {
                milestone.Property(m => m.RepositoryId).IsRequired();
                milestone.Property(m => m.Title).IsRequired();
                milestone.Property(i => i.State).HasConversion(new EnumToStringConverter<ItemState>());

                milestone
                    .HasOne(m => m.Repository)
                    .WithMany(r => r!.Milestones)
                    .HasForeignKey(m => m.RepositoryId)
                    .IsRequired();

                milestone.ToTable("Milestones");
            });

            modelBuilder.Entity<IssueLabel>(issueLabel =>
            {
                issueLabel.HasKey(i => new { i.IssueId, i.LabelId });

                issueLabel
                    .HasOne(i => i.Issue)
                    .WithMany(i => i!.Labels)
                    .HasForeignKey(i => i.IssueId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                issueLabel
                    .HasOne(i => i.Label)
                    .WithMany(a => a!.Issues)
                    .HasForeignKey(i => i.LabelId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                issueLabel.ToTable("IssueLabels");
            });

            modelBuilder.Entity<IssueLink>(issueLink =>
            {
                issueLink.Property(l => l.LinkType).IsRequired();

                issueLink
                    .HasOne(l => l.TargetRepository)
                    .WithMany()
                    .HasForeignKey(i => i.RepositoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                issueLink
                    .HasOne(i => i.Issue)
                    .WithMany(i => i!.OutboundLinks)
                    .HasForeignKey(i => i.IssueId)
                    .OnDelete(DeleteBehavior.Cascade);


                issueLink.ToTable("IssueLinks");
            });
        }
}
}
