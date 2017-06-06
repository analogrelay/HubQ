using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public class SyncCommand
    {
        private readonly string _sqlConnectionString;
        private readonly IList<string> _repositories;
        private readonly string _agent;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SyncCommand> _logger;
        private readonly HubSyncContext _context;
        private readonly Octokit.IGitHubClient _github;

        private Dictionary<int, int> _userCache = new Dictionary<int, int>();
        private Dictionary<Tuple<int, string>, int> _labelCache = new Dictionary<Tuple<int, string>, int>();
        private Dictionary<Tuple<int, int>, int> _milestoneCache = new Dictionary<Tuple<int, int>, int>();

        public SyncCommand(Octokit.Credentials gitHubCredentials, string sqlConnectionString, IList<string> repositories, string agent, ILoggerFactory loggerFactory)
        {
            _sqlConnectionString = sqlConnectionString;
            _repositories = repositories;
            _agent = agent;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<SyncCommand>();

            // Set up EF
            var options = new DbContextOptionsBuilder<HubSyncContext>()
                .UseSqlServer(_sqlConnectionString)
                .Options;
            _context = new HubSyncContext(options);

            // Set up GitHub
            _github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("HubSync", Program.Version));
            _github.Connection.Credentials = gitHubCredentials;

            if (repositories.Count == 0)
            {
                throw new CommandLineException("At least one repository must be specified");
            }
        }

        public async Task<int> ExecuteAsync()
        {
            _logger.LogInformation("Checking database migration status ...");

            // Check for outstanding migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                return PendingMigrationsError(pendingMigrations);
            }

            _logger.LogInformation("Database schema is up-to-date.");

            // Sync each repo
            foreach (var repo in _repositories)
            {
                if (!await TrySyncRepo(repo))
                {
                    return 1;
                }
            }

            _logger.LogInformation("HubSync has finished.");
            return 0;
        }

        private async Task<bool> TrySyncRepo(string repoName)
        {
            _logger.LogInformation("Syncing issues for {repo}", repoName);
            var repo = await GetOrCreateRepoAsync(repoName);

            // Get the sync history entry for this repo
            var (success, syncTime) = await GetLastSyncTimeAsync(repo);
            if (!success)
            {
                return false;
            }

            // Create the new sync history entry
            var syncHistory = await RecordStartSync(repo);

            try
            {
                // Fetch all issues modified since the last sync
                var request = CreateIssueRequest(syncTime);

                // Fetch the data
                var apiOptions = new Octokit.ApiOptions()
                {
                    PageSize = 100
                };

                var issues = await _github.Issue.GetAllForRepository(repo.Owner, repo.Name, request, apiOptions);

                // Grab API info (for rate limit impact)
                var apiInfo = _github.GetLastApiInfo();
                _logger.LogInformation("Fetched issues for '{repo}'. Rate Limit remaining: {remainingRateLimit}", repo, apiInfo.RateLimit.Remaining);

                await IngestIssuesAsync(issues, repo, syncTime);

                // Update sync history
                syncHistory.Status = SyncStatus.Synchronized;
                syncHistory.CompletedUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Record the error
                syncHistory.Error = ex.ToString();
                syncHistory.Status = SyncStatus.Failed;
                await _context.SaveChangesAsync();

                throw;
            }

            return true;
        }

        private async Task IngestIssuesAsync(IReadOnlyList<Octokit.Issue> issues, Repository repo, DateTime lastSyncTime)
        {
            foreach (var issue in issues)
            {
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

                issueModel.Body = issue.Body;
                issueModel.ClosedAt = issue.ClosedAt;
                issueModel.CommentCount = issue.Comments;
                issueModel.CreatedAt = issue.CreatedAt;
                issueModel.GitHubId = issue.Id;
                issueModel.HtmlUrl = issue.HtmlUrl.ToString();
                issueModel.Locked = issue.Locked;
                issueModel.Number = issue.Number;
                issueModel.PullRequestUrl = issue.PullRequest?.HtmlUrl?.ToString();
                issueModel.RepositoryId = repo.Id;
                issueModel.State = issue.State == Octokit.ItemState.Open ? IssueState.Open : IssueState.Closed;
                issueModel.Title = issue.Title;
                issueModel.UpdatedAt = issue.UpdatedAt;
                issueModel.Url = issue.Url?.ToString();

                if (issue.Milestone != null)
                {
                    issueModel.MilestoneId = await GetOrCreateMilestoneAsync(issue.Milestone, repo.Id);
                }

                if (issue.ClosedBy != null)
                {
                    issueModel.ClosedById = await GetOrCreateUserAsync(issue.ClosedBy);
                }

                if (issue.User != null)
                {
                    issueModel.UserId = await GetOrCreateUserAsync(issue.User);
                }

                await SyncList(
                    issueModel.Assignees,
                    issue.Assignees,
                    _context.IssueAssignees,
                    equalityComparer: (left, right) => left.Id == right.User.GitHubId,
                    createItem: async assignee => new IssueAssignee()
                    {
                        UserId = await GetOrCreateUserAsync(assignee)
                    });

                await SyncList(
                    issueModel.Labels,
                    issue.Labels,
                    _context.IssueLabels,
                    equalityComparer: (left, right) => {
                        if(right.Label == null)
                        {
                            Console.WriteLine("Label is null! Left side was: " + left.Name);
                        }
                        return left.Name == right.Label.Name;
                    },
                    createItem: async l => new IssueLabel()
                    {
                        LabelId = await GetOrCreateLabelAsync(l, repo.Id)
                    });

                if (newIssue)
                {
                    _context.Issues.Add(issueModel);
                }

                stopwatch.Stop();
                await _context.SaveChangesAsync();
                _logger.LogTrace("Synced issue #{number} - {title} in {elapsedMs:0.00}ms", issue.Number, issue.Title, stopwatch.ElapsedMilliseconds);
            }
        }

        private static async Task SyncList<TSource, TTarget>(
            IList<TTarget> targetList,
            IReadOnlyList<TSource> sourceList,
            DbSet<TTarget> contextList,
            Func<TSource, TTarget, bool> equalityComparer,
            Func<TSource, Task<TTarget>> createItem) where TTarget: class
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

            foreach (var sourceItem in sourceList)
            {
                if (!targetList.Any(u => equalityComparer(sourceItem, u)))
                {
                    targetList.Add(await createItem(sourceItem));
                }
            }
        }

        private async Task<int> GetOrCreateLabelAsync(Octokit.Label githubLabel, int repoId)
        {
            if (_labelCache.TryGetValue(Tuple.Create(repoId, githubLabel.Name), out var labelId))
            {
                return labelId;
            }

            var label = await _context.Labels
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
                await _context.SaveChangesAsync();
            }

            _labelCache[Tuple.Create(repoId, label.Name)] = label.Id;
            return label.Id;
        }

        private async Task<int> GetOrCreateMilestoneAsync(Octokit.Milestone githubMilestone, int repoId)
        {
            if (_milestoneCache.TryGetValue(Tuple.Create(repoId, githubMilestone.Number), out var milestoneId))
            {
                return milestoneId;
            }

            var milestone = await _context.Milestones
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
                await _context.SaveChangesAsync();
            }

            _milestoneCache[Tuple.Create(repoId, milestone.Number)] = milestone.Id;
            return milestone.Id;
        }

        private async Task<int> GetOrCreateUserAsync(Octokit.User githubUser)
        {
            if (_userCache.TryGetValue(githubUser.Id, out var userId))
            {
                return userId;
            }

            var user = await _context.Users
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
                await _context.SaveChangesAsync();
            }

            _userCache[user.GitHubId] = user.Id;
            return user.Id;
        }

        private async Task<Repository> GetOrCreateRepoAsync(string fullName)
        {
            var splat = fullName.Split('/');
            if (splat.Length != 2)
            {
                throw new FormatException($"Invalid Repository Name: {fullName}");
            }
            var (owner, name) = (splat[0].ToLowerInvariant(), splat[1].ToLowerInvariant());

            var repo = await _context.Repositories
                .FirstOrDefaultAsync(r => r.Owner == owner && r.Name == name);
            if (repo == null)
            {
                repo = new Repository()
                {
                    Owner = owner.ToLowerInvariant(),
                    Name = name.ToLowerInvariant()
                };
                _context.Repositories.Add(repo);
                await _context.SaveChangesAsync();
            }
            return repo;
        }

        private Octokit.RepositoryIssueRequest CreateIssueRequest(DateTime syncTime)
        {
            var request = new Octokit.RepositoryIssueRequest();
            if (syncTime != DateTime.MinValue)
            {
                _logger.LogInformation("Last sync was at {syncTime}. Fetching changes since then.", syncTime.ToLocalTime());
                request.Since = new DateTimeOffset(syncTime, TimeSpan.Zero);
            }
            return request;
        }

        private async Task<SyncHistory> RecordStartSync(Repository repo)
        {
            var syncHistory = new SyncHistory()
            {
                CreatedUtc = DateTime.UtcNow,
                RepositoryId = repo.Id,
                Status = SyncStatus.Synchronizing,
                Agent = _agent
            };
            _context.SyncHistory.Add(syncHistory);
            await _context.SaveChangesAsync();
            return syncHistory;
        }

        private async Task<(bool success, DateTime syncTime)> GetLastSyncTimeAsync(Repository repo)
        {
            // Find the last successful non-failed status.
            var latest = await _context.SyncHistory
                .Where(h => h.RepositoryId == repo.Id && h.Status != SyncStatus.Failed)
                .OrderByDescending(h => h.CompletedUtc)
                .FirstOrDefaultAsync();
            if (latest != null && latest.Status != SyncStatus.Synchronized)
            {
                _logger.LogError("A synchronization is already underway by agent '{agent}'.", latest.Agent);
                _logger.LogError("If you need to forcibly cancel it, run the 'cancel' command. This will NOT stop active processes attempting to synchronize it.");
                return (false, DateTime.MinValue);
            }
            return (true, latest?.CreatedUtc ?? DateTime.MinValue);
        }

        private int PendingMigrationsError(IEnumerable<string> pendingMigrations)
        {
            _logger.LogError("Unable to continue, there are pending migrations.");
            foreach (var migration in pendingMigrations)
            {
                _logger.LogError("Pending Migration: {migrationName}", migration);
            }
            return 1;
        }
    }
}
