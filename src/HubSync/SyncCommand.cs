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
        private readonly SyncTarget _syncTarget;
        private readonly IReadOnlyList<string> _repositories;
        private readonly string _agent;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SyncCommand> _logger;
        private readonly Octokit.IGitHubClient _github;

        public SyncCommand(Octokit.Credentials gitHubCredentials, SyncTarget syncTarget, IReadOnlyList<string> repositories, string agent, ILoggerFactory loggerFactory)
        {
            _syncTarget = syncTarget;
            _repositories = repositories;
            _agent = agent;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<SyncCommand>();

            // Set up GitHub
            _github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("HubSync", Program.Version));
            _github.Connection.Credentials = gitHubCredentials;

            if (_repositories.Count == 0)
            {
                throw new CommandLineException("At least one repository must be specified");
            }
        }

        public async Task<int> ExecuteAsync()
        {
            _logger.LogDebug("Checking database schema status ...");

            // Verify database is ready
            if (!await _syncTarget.IsSchemaUpToDateAsync())
            {
                return 1;
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

            var splat = repoName.Split('/');
            if (splat.Length != 2)
            {
                throw new FormatException($"Invalid Repository Name: {repoName}");
            }
            var githubRepo = await _github.Repository.Get(splat[0].ToLowerInvariant(), splat[1].ToLowerInvariant());
            var repo = await _syncTarget.GetOrCreateRepoAsync(githubRepo);

            // Get the sync history entry for this repo
            var syncRecord = await _syncTarget.GetLastSyncRecordAsync(repo);
            var syncTime = syncRecord?.CreatedUtc ?? DateTime.MinValue;

            // Create the new sync history entry
            var syncHistory = await _syncTarget.RecordStartSyncAsync(repo, _agent);

            try
            {
                // Fetch the data
                var apiOptions = new Octokit.ApiOptions()
                {
                    PageSize = 100
                };

                var issues = await _github.Issue.GetAllForRepository(repo.Owner, repo.Name, CreateIssueRequest(syncTime), apiOptions);

                // Grab API info (for rate limit impact)
                var apiInfo = _github.GetLastApiInfo();
                _logger.LogInformation("Fetched {issueCount} issues for '{repo}'. Rate Limit remaining: {remainingRateLimit}", issues.Count, $"{repo.Owner}/{repo.Name}", apiInfo.RateLimit.Remaining);

                await IngestIssuesAsync(issues, repo, syncTime);

                _logger.LogInformation("Synced {issueCount} issues.", issues.Count);

                // Update sync history
                syncHistory.Status = SyncStatus.Synchronized;
                syncHistory.CompletedUtc = DateTime.UtcNow;
                await _syncTarget.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Record the error
                syncHistory.Error = ex.ToString();
                syncHistory.Status = SyncStatus.Failed;
                await _syncTarget.SaveChangesAsync();

                throw;
            }

            return true;
        }

        private async Task IngestIssuesAsync(IReadOnlyList<Octokit.Issue> issues, Repository repo, DateTime lastSyncTime)
        {
            foreach (var issue in issues)
            {
                await IngestIssueAsync(repo, issue);
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Saving issues to database...");
            await _syncTarget.SaveChangesAsync();
            stopwatch.Stop();
            _logger.LogInformation("Saved {issueCount} issues in {elapsedMs:0.00}ms", issues.Count, stopwatch.ElapsedMilliseconds);
        }

        private async Task IngestIssueAsync(Repository repo, Octokit.Issue issue)
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

            issueModel.PullRequest = await CreatePullRequestAsync(repo, issue.PullRequest);

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

        private async Task<PullRequest> CreatePullRequestAsync(Models.Repository repo, Octokit.PullRequest pr)
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

        private Octokit.RepositoryIssueRequest CreateIssueRequest(DateTime syncTime)
        {
            var request = new Octokit.RepositoryIssueRequest()
            {
                State = Octokit.ItemStateFilter.All
            };

            if (syncTime != DateTime.MinValue)
            {
                _logger.LogInformation("Last sync was at {syncTime}. Fetching changes since then.", syncTime.ToLocalTime());
                request.Since = new DateTimeOffset(syncTime, TimeSpan.Zero);
            }
            return request;
        }
    }
}
