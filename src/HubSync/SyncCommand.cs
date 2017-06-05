using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public class SyncCommand
    {
        private readonly string _userName;
        private readonly string _token;
        private readonly string _sqlConnectionString;
        private readonly IList<string> _repositories;
        private readonly string _agent;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SyncCommand> _logger;
        private readonly HubSyncContext _context;
        private readonly Octokit.IGitHubClient _github;

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

            // Fetch all issues modified since the last sync
            var request = CreateIssueRequest(syncTime);

            // Fetch the data
            var apiOptions = new Octokit.ApiOptions()
            {
                PageSize = 100
            };

            var issues = await _github.Issue.GetAllForRepository(repo.Owner, repo.Name, request, apiOptions);

            await IngestIssuesAsync(issues, repo, syncTime);

            // Grab API info (for rate limit impact)
            var apiInfo = _github.GetLastApiInfo();
            _logger.LogInformation("Synced issues for '{repo}'. Rate Limit remaining: {remainingRateLimit}", repo, apiInfo.RateLimit.Remaining);

            return true;
        }

        private async Task IngestIssuesAsync(IReadOnlyList<Octokit.Issue> issues, Repository repo, DateTime lastSyncTime)
        {
            foreach (var issue in issues)
            {
                // Check if we need to create a new issue
                Issue issueModel;
                if (issue.CreatedAt < lastSyncTime)
                {
                    // We should have an issue already
                    issueModel = await _context.Issues
                        .FirstOrDefaultAsync(i => i.RepositoryId == repo.Id && i.Number == issue.Number);
                }
                else
                {
                    issueModel = new Issue()
                    {
                        RepositoryId = repo.Id
                    };
                }

                issueModel.Body = issue.Body;
                issueModel.ClosedAt = issue.ClosedAt;
                issueModel.ClosedById = (await GetOrCreateUserAsync(issue.ClosedBy)).Id;
                issueModel.CommentCount = issue.Comments;
                issueModel.CreatedAt = issue.CreatedAt;
                issueModel.GitHubId = issue.Id;
                issueModel.HtmlUrl = issue.HtmlUrl.ToString();
                issueModel.Locked = issue.Locked;
                issueModel.MilestoneId = (await GetOrCreateMilestoneAsync(issue.Milestone)).Id;
                issueModel.Number = issue.Number;
                issueModel.PullRequestUrl = issue.PullRequest?.HtmlUrl;
                issueModel.RepositoryId = repo.Id,
                issueModel.State = issue.State == Octokit.ItemState.Open ? IssueState.Open : IssueState.Closed;
                issueModel.Title = issue.Title;
                issueModel.UpdatedAt = issue.UpdatedAt;
                issueModel.Url = issue.Url.ToString();
                issueModel.UserId = (await GetOrCreateUserAsync(issue.User)).Id;

                // Assignees and labels TODO
                issueModel.Assignees = ?;
                issueModel.Labels = ?;
            }
        }

        private async Task<Milestone> GetOrCreateMilestoneAsync(Octokit.Milestone githubMilestone)
        {
            var milestone = await _context.Milestones
                .FirstOrDefaultAsync(m => m.GitHubId == githubMilestone.Id);
            if (milestone == null)
            {
                milestone = new Milestone()
                {
                    GitHubId = githubMilestone.Id,
                    Title = githubMilestone.Title
                };
                _context.Milestones.Add(milestone);
                await _context.SaveChangesAsync();
            }
            return milestone;
        }

        private async Task<User> GetOrCreateUserAsync(Octokit.User githubUser)
        {
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
            return user;
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

        private static Octokit.RepositoryIssueRequest CreateIssueRequest(DateTime syncTime)
        {
            var request = new Octokit.RepositoryIssueRequest();
            if (syncTime != DateTime.MinValue)
            {
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
            // Normalize the repo name
            var latest = await _context.SyncHistory
                .Where(h => h.RepositoryId == repo.Id)
                .OrderByDescending(h => h.CompletedUtc)
                .FirstOrDefaultAsync();
            if (latest?.Status != SyncStatus.Synchronized)
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
