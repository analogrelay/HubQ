using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HubSync.Models;
using HubSync.Models.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public class SqlSyncTarget : SyncTarget
    {
        private readonly Models.Sql.HubSyncContext _context;
        private readonly ILogger<SqlSyncTarget> _logger;

        private Dictionary<int, Models.Sql.User> _userCache = new Dictionary<int, Models.Sql.User>();
        private Dictionary<Tuple<int, string>, Models.Sql.Label> _labelCache = new Dictionary<Tuple<int, string>, Models.Sql.Label>();
        private Dictionary<Tuple<int, int>, Models.Sql.Milestone> _milestoneCache = new Dictionary<Tuple<int, int>, Models.Sql.Milestone>();
        private Dictionary<Tuple<string, string>, Models.Sql.Repository> _repoCache = new Dictionary<Tuple<string, string>, Models.Sql.Repository>();

        private SyncHistory _currentSync;

        public SqlSyncTarget(string sqlConnectionString, ILoggerFactory loggerFactory)
        {
            // Set up EF
            var options = new DbContextOptionsBuilder<Models.Sql.HubSyncContext>()
                .EnableSensitiveDataLogging(true)
                .UseLoggerFactory(loggerFactory)
                .UseSqlServer(sqlConnectionString)
                .Options;
            _context = new Models.Sql.HubSyncContext(options);
            _logger = loggerFactory.CreateLogger<SqlSyncTarget>();
        }

        public override async Task<DateTime?> GetLastSyncTimeAsync(string owner, string name)
        {
            var repo = await GetOrCreateRepoAsync(owner, name);

            // Find the last successful non-failed status.
            var latest = await _context.SyncHistory
                .Where(h => h.RepositoryId == repo.Id && h.Status != SyncStatus.Failed)
                .OrderByDescending(h => h.CompletedUtc)
                .FirstOrDefaultAsync();
            if (latest != null && latest.Status != SyncStatus.Synchronized)
            {
                _logger.LogError("A synchronization is already underway by agent '{agent}'.", latest.Agent);
                _logger.LogError("If you need to forcibly cancel it, run the 'cancel' command.");
                _logger.LogError("NOTE: This will NOT stop active processes associated with the sync.");
                return null;
            }
            return latest?.CreatedUtc ?? DateTime.MinValue;
        }

        public override async Task<bool> IsSchemaUpToDateAsync()
        {
            // Check for outstanding migrations
            _logger.LogDebug("Checking for pending migrations...");
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                _logger.LogError("Unable to continue, there are pending migrations.");
                foreach (var migration in pendingMigrations)
                {
                    _logger.LogError("Pending Migration: {migrationName}", migration);
                }
                return false;
            }
            _logger.LogDebug("No pending migrations found");
            return true;
        }

        public override async Task RecordStartSyncAsync(string owner, string name, string agent)
        {
            if (_currentSync != null)
            {
                throw new InvalidOperationException($"Can't call {nameof(RecordStartSyncAsync)} twice.");
            }

            var repo = await GetOrCreateRepoAsync(owner, name);

            var syncHistory = new Models.Sql.SyncHistory()
            {
                CreatedUtc = DateTime.UtcNow,
                RepositoryId = repo.Id,
                Status = SyncStatus.Synchronizing,
                Agent = agent
            };
            _context.SyncHistory.Add(syncHistory);
            _currentSync = syncHistory;
            await _context.SaveChangesAsync();
        }

        public override async Task CompleteSyncAsync(string error)
        {
            if (_currentSync == null)
            {
                throw new InvalidOperationException($"Can't call {nameof(CompleteSyncAsync)} unless {nameof(RecordStartSyncAsync)} has been called");
            }

            _currentSync.CompletedUtc = DateTime.UtcNow;
            if (string.IsNullOrEmpty(error))
            {
                _currentSync.Status = SyncStatus.Synchronized;
            }
            else
            {
                _currentSync.Status = SyncStatus.Failed;
                _currentSync.Error = error;
            }
            await _context.SaveChangesAsync();
        }

        public override async Task SyncIssueAsync(Octokit.Issue issue)
        {
            var repo = await GetOrCreateRepoAsync(issue.Repository.Owner.Location, issue.Repository.Name);

            var stopwatch = Stopwatch.StartNew();

            // Check if we need to create a new issue
            var issueModel = await _context.Issues
                .Include(i => i.Assignees).ThenInclude(a => a.User)
                .Include(i => i.Labels).ThenInclude(l => l.Label)
                .FirstOrDefaultAsync(i => i.RepositoryId == repo.Id && i.Number == issue.Number);
            var newIssue = false;
            if (issueModel == null)
            {
                issueModel = new Issue()
                {
                    RepositoryId = repo.Id
                };
                newIssue = true;
            }

            issueModel.PullRequest = await CreatePullRequestAsync(issue.PullRequest);

            issueModel.Body = issue.Body;
            issueModel.ClosedAt = issue.ClosedAt;
            issueModel.CommentCount = issue.Comments;
            issueModel.CreatedAt = issue.CreatedAt;
            issueModel.GitHubId = issue.Id;
            issueModel.HtmlUrl = issue.HtmlUrl.ToString();
            issueModel.Locked = issue.Locked;
            issueModel.Number = issue.Number;
            issueModel.RepositoryId = repo.Id;
            issueModel.State = issue.State == Octokit.ItemState.Open ? IssueState.Open : IssueState.Closed;
            issueModel.Title = issue.Title;
            issueModel.UpdatedAt = issue.UpdatedAt;
            issueModel.Url = issue.Url?.ToString();

            if (issue.Milestone != null)
            {
                issueModel.Milestone = await GetOrCreateMilestoneAsync(issue.Milestone, repo.Id);
            }

            if (issue.ClosedBy != null)
            {
                issueModel.ClosedBy = await GetOrCreateUserAsync(issue.ClosedBy);
            }

            if (issue.User != null)
            {
                issueModel.User = await GetOrCreateUserAsync(issue.User);
            }

            issueModel.Reactions = CreateReactions(issue.Reactions);

            await SyncList(
                issueModel.Assignees,
                issue.Assignees,
                _context.IssueAssignees,
                equalityComparer: (left, right) => left.Id == right.User.GitHubId,
                createItem: async assignee => new IssueAssignee()
                {
                    User = await GetOrCreateUserAsync(assignee)
                });

            await SyncList(
                issueModel.Labels,
                issue.Labels,
                _context.IssueLabels,
                equalityComparer: (left, right) =>
                {
                    if (right.Label == null)
                    {
                        Console.WriteLine("Label is null! Left side was: " + left.Name);
                    }
                    return left.Name == right.Label.Name;
                },
                createItem: async l => new IssueLabel()
                {
                    Label = await GetOrCreateLabelAsync(l, repo.Id)
                });

            if (newIssue)
            {
                _context.Issues.Add(issueModel);
            }

            stopwatch.Stop();
            _logger.LogTrace((newIssue ? "Added" : "Updated") + " issue #{number} - {title} in {elapsedMs:0.00}ms", issue.Number, issue.Title, stopwatch.ElapsedMilliseconds);
        }

        private async Task<Repository> GetOrCreateRepoAsync(string owner, string name)
        {
            if (!_repoCache.TryGetValue(Tuple.Create(owner, name), out var repo))
            {
                repo = await _context.Repositories
                    .FirstOrDefaultAsync(r => r.Owner == owner && r.Name == name);

                if (repo == null)
                {
                    repo = new Repository()
                    {
                        Owner = owner.ToLowerInvariant(),
                        Name = name.ToLowerInvariant()
                    };
                    _context.Repositories.Add(repo);
                }

                _repoCache[Tuple.Create(owner, name)] = repo;
            }
            return repo;
        }

        private Reactions CreateReactions(Octokit.ReactionSummary reactions)
        {
            if (reactions == null)
            {
                return new Reactions();
            }
            else
            {
                return new Reactions()
                {
                    Confused = reactions.Confused,
                    Plus1 = reactions.Plus1,
                    Minus1 = reactions.Minus1,
                    Hooray = reactions.Hooray,
                    Heart = reactions.Heart,
                    Laugh = reactions.Laugh,
                    TotalCount = reactions.TotalCount
                };
            }
        }

        private async Task<PullRequest> CreatePullRequestAsync(Octokit.PullRequest pr)
        {
            if (pr == null)
            {
                return new PullRequest()
                {
                    IsPr = false
                };
            }
            else
            {
                var prModel = new PullRequest()
                {
                    IsPr = true,
                    BaseRef = pr.Base?.Ref,
                    BaseSha = pr.Base?.Sha,
                    ChangedFiles = pr.ChangedFiles,
                    HeadRef = pr.Head?.Ref,
                    HeadSha = pr.Head?.Sha,
                    Mergeable = pr.Mergeable,
                    MergedAt = pr.MergedAt,
                };

                if (pr.MergedBy != null)
                {
                    prModel.MergedBy = await GetOrCreateUserAsync(pr.MergedBy);
                }

                return prModel;
            }
        }

        private static async Task SyncList<TSource, TTarget>(
            IList<TTarget> targetList,
            IReadOnlyList<TSource> sourceList,
            DbSet<TTarget> contextList,
            Func<TSource, TTarget, bool> equalityComparer,
            Func<TSource, Task<TTarget>> createItem) where TTarget : class
        {
            var toRemove = new List<TTarget>();
            foreach (var targetItem in targetList)
            {
                if (!sourceList.Any(u => equalityComparer(u, targetItem)))
                {
                    // Remove this one
                    toRemove.Add(targetItem);
                }
            }

            foreach (var removedItem in toRemove)
            {
                targetList.Remove(removedItem);
                contextList.Remove(removedItem);
            }

            var toAdd = new List<TTarget>();
            foreach (var sourceItem in sourceList)
            {
                if (!targetList.Any(u => equalityComparer(sourceItem, u)))
                {
                    // Don't add directly because this item won't be usable in the equality comparer.
                    toAdd.Add(await createItem(sourceItem));
                }
            }

            foreach (var addedItem in toAdd)
            {
                targetList.Add(addedItem);
            }
        }

        private async Task<Label> GetOrCreateLabelAsync(Octokit.Label githubLabel, int repoId)
        {
            if (_labelCache.TryGetValue(Tuple.Create(repoId, githubLabel.Name), out var label))
            {
                return label;
            }

            label = await _context.Labels
                .FirstOrDefaultAsync(l => l.RepositoryId == repoId && l.Name == githubLabel.Name);
            if (label == null)
            {
                label = new Label()
                {
                    RepositoryId = repoId,
                    Name = githubLabel.Name,
                    Color = githubLabel.Color
                };
                _context.Labels.Add(label);
            }

            _labelCache[Tuple.Create(repoId, label.Name)] = label;
            return label;
        }

        private async Task<Milestone> GetOrCreateMilestoneAsync(Octokit.Milestone githubMilestone, int repoId)
        {
            if (_milestoneCache.TryGetValue(Tuple.Create(repoId, githubMilestone.Number), out var milestone))
            {
                return milestone;
            }

            milestone = await _context.Milestones
                .FirstOrDefaultAsync(m => m.RepositoryId == repoId && m.Number == githubMilestone.Number);
            if (milestone == null)
            {
                milestone = new Milestone()
                {
                    RepositoryId = repoId,
                    Number = githubMilestone.Number,
                    Title = githubMilestone.Title
                };
                _context.Milestones.Add(milestone);
            }

            _milestoneCache[Tuple.Create(repoId, milestone.Number)] = milestone;
            return milestone;
        }

        private async Task<User> GetOrCreateUserAsync(Octokit.User githubUser)
        {
            if (_userCache.TryGetValue(githubUser.Id, out var user))
            {
                return user;
            }

            user = await _context.Users
                .FirstOrDefaultAsync(r => r.Login == githubUser.Login);
            if (user == null)
            {
                user = new User()
                {
                    GitHubId = githubUser.Id,
                    Login = githubUser.Login,
                    AvatarUrl = githubUser.AvatarUrl
                };
                _context.Users.Add(user);
            }

            _userCache[user.GitHubId] = user;
            return user;
        }
    }
}