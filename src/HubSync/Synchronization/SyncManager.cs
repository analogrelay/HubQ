using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Octokit;

namespace HubSync.Synchronization
{
    internal class SyncManager
    {
        public HubSyncContext Db { get; }
        public GitHubClient GitHub { get; }

        private Dictionary<string, Actor> _actorCache = new Dictionary<string, Actor>();
        private Dictionary<long, Models.Repository> _repoCache = new Dictionary<long, Models.Repository>();
        private Dictionary<long, Models.Issue> _issueCache = new Dictionary<long, Models.Issue>();
        private Dictionary<string, Models.Label> _labelCache = new Dictionary<string, Models.Label>();
        private Dictionary<string, Models.Milestone> _milestoneCache = new Dictionary<string, Models.Milestone>();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SyncManager> _logger;

        public SyncManager(HubSyncContext db, GitHubClient github, ILoggerFactory loggerFactory)
        {
            Db = db;
            GitHub = github;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<SyncManager>();
        }

        public async ValueTask<Actor> SyncActorAsync(Octokit.User user)
        {
            Actor model;
            if (_actorCache.TryGetValue(user.Login, out model))
            {
                _logger.LogTrace("Loaded user {Login} from cache.", user.Login);
            }
            else
            {
                model = await Db.Actors.FirstOrDefaultAsync(a => a.GitHubId == user.Id);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new user {Login}.", user.Login);
                    model = new Actor();
                    Db.Actors.Add(model);
                }
            }

            model.UpdateFrom(user);

            _actorCache[user.Login] = model;
            return model;
        }

        public async ValueTask<Models.Repository> SyncRepoAsync(Octokit.Repository repo)
        {
            Models.Repository model;
            if (_repoCache.TryGetValue(repo.Id, out model))
            {
                _logger.LogTrace("Loaded repo {Owner}/{Name} from cache.", model.Owner, model.Name);
            }
            else
            {
                model = await Db.Repositories.FirstOrDefaultAsync(r => r.GitHubId == repo.Id);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new repo {Owner}/{Name}.", repo.Owner.Login, repo.Name);
                    model = new Models.Repository();
                    Db.Repositories.Add(model);
                }
            }

            model.UpdateFrom(repo);

            _repoCache[model.GitHubId] = model;
            return model;
        }

        public async ValueTask<Models.Issue> SyncIssueAsync(Models.Repository repo, Octokit.Issue issue)
        {
            Models.Issue model;
            if (_issueCache.TryGetValue(issue.Id, out model))
            {
                _logger.LogTrace("Loaded issue {Owner}/{Name}#{Number} from cache.", repo.Owner, repo.Name, issue.Number);
            }
            else
            {
                model = await Db.Issues
                    .Include(i => i.Assignees!).ThenInclude((IssueAssignee a) => a.Assignee)
                    .Include(i => i.Labels!).ThenInclude((IssueLabel l) => l.Label)
                    .FirstOrDefaultAsync(i => i.GitHubId == issue.Id);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new issue {Owner}/{Name}#{Number}.", repo.Owner, repo.Name, issue.Number);
                    model = new Models.Issue();
                    Db.Issues.Add(model);
                }
            }

            model.UpdateFrom(issue);

            // Update the navigation properties.
            var author = await SyncActorAsync(issue.User);
            model.Author = author;
            model.Repository = repo;

            // Update milestone
            if (issue.Milestone != null)
            {
                var milestone = await SyncMilestoneAsync(repo, issue.Milestone);
                model.Milestone = milestone;
            }

            // Update assignees
            this logic doesnt properly handle merging/purging data :(
            if (model.Assignees == null)
            {
                model.Assignees = new List<IssueAssignee>();
            }
            else
            {
                model.Assignees.Clear();
            }
            foreach (var assignee in issue.Assignees)
            {
                var actor = await SyncActorAsync(assignee);
                var issueAssignee = new IssueAssignee()
                {
                    Issue = model,
                    Assignee = actor,
                };
                Db.IssueAssignees.Add(issueAssignee);
                model.Assignees.Add(issueAssignee);
            }

            // Update labels
            if (model.Labels == null)
            {
                model.Labels = new List<IssueLabel>();
            }
            else
            {
                model.Labels.Clear();
            }
            foreach (var label in issue.Labels)
            {
                var labelModel = await SyncLabelAsync(repo, label);
                var issueLabel = new IssueLabel()
                {
                    Issue = model,
                    Label = labelModel,
                };
                Db.IssueLabels.Add(issueLabel);
                model.Labels.Add(issueLabel);
            }

            // Scan the body for outbound links
            if (model.OutboundLinks == null)
            {
                model.OutboundLinks = new List<IssueLink>();
            }
            else
            {
                model.OutboundLinks.Clear();
            }

            foreach (var (linkType, owner, repoName, number) in ScanForLinks(issue.Body))
            {
                var link = new IssueLink()
                {
                    LinkType = linkType,
                    RepoOwner = string.IsNullOrEmpty(owner) ? repo.Owner : owner,
                    RepoName = string.IsNullOrEmpty(repoName) ? repo.Name : repoName,
                    Number = number
                };
                Db.IssueLinks.Add(link);
                model.OutboundLinks.Add(link);
            }

            _issueCache[model.GitHubId] = model;
            return model;
        }

        // I think this regex speaks for itself -nobody ever
        // It's for parsing GitHub issue links in each of the following forms:
        // * '#1234' - In-repo short-form links
        // * 'owner/repo#1234' - Cross-repo short-form links
        // * 'http://github.com/owner/repo/issues/1234/' - URL-based links (also supports HTTPS and "www." prefix as well as the optional trailing '/')
        private static readonly Regex _issueLinkScanner = new Regex(
            @"^(?<linkType>[^\s]+)\s+((https?://(www\.)?github.com/(?<owner>[a-zA-Z0-9-_]+)/(?<repo>[a-zA-Z0-9-_]+)/issues/(?<number>[0-9]+)/?)|(((?<owner>[a-zA-Z0-9-_]+ )/(?<repo>[a-zA-Z0-9-_]+))?#(?<number>[0-9]+)))\s*$",
            RegexOptions.Multiline);
        private IEnumerable<(string LinkType, string Owner, string RepoName, int Number)> ScanForLinks(string body)
        {
            // Ahh, ye olde implementors of IEnumerable (non-generic)
            var matches = _issueLinkScanner.Matches(body).Cast<Match>();
            foreach (var match in matches)
            {
                var linkType = match.Groups["linkType"].Value;
                var owner = match.Groups["owner"].Value;
                var repoName = match.Groups["repo"].Value;
                var number = int.Parse(match.Groups["number"].Value);
                yield return (linkType, owner, repoName, number);
            }
        }

        public async ValueTask<Models.Label> SyncLabelAsync(Models.Repository repo, Octokit.Label label)
        {
            Models.Label model;
            if (_labelCache.TryGetValue(label.NodeId, out model))
            {
                _logger.LogTrace("Loaded label {Name} from cache.", label.Name);
            }
            else
            {
                model = await Db.Labels
                    .FirstOrDefaultAsync(i => i.NodeId == label.NodeId);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new label {Name}.", label.Name);
                    model = new Models.Label();
                    Db.Labels.Add(model);
                }
            }

            model.UpdateFrom(label);
            model.Repository = repo;

            _labelCache[label.NodeId] = model;

            return model;
        }

        public async ValueTask<Models.Milestone> SyncMilestoneAsync(Models.Repository repo, Octokit.Milestone milestone)
        {
            Models.Milestone model;
            if (_milestoneCache.TryGetValue(milestone.NodeId, out model))
            {
                _logger.LogTrace("Loaded milestone {Title} from cache.", milestone.Title);
            }
            else
            {
                model = await Db.Milestones
                    .FirstOrDefaultAsync(i => i.NodeId == milestone.NodeId);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new milestone {Title}.", milestone.Title);
                    model = new Models.Milestone();
                    Db.Milestones.Add(model);
                }
            }

            model.UpdateFrom(milestone);
            model.Repository = repo;

            _milestoneCache[milestone.NodeId] = model;

            return model;
        }

        public async Task SaveChangesAsync()
        {
            var sw = ValueStopwatch.StartNew();
            _logger.LogDebug("Saving changes to database...");
            await Db.SaveChangesAsync();
            var elapsed = sw.GetElapsedTime();
            _logger.LogDebug("Saved changes in {Elapsed}ms", elapsed.TotalMilliseconds);
        }

        public async Task<SyncContext> CreateSyncContextAsync(Models.Repository repo, Actor user)
        {
            // Get the latest sync log
            _logger.LogDebug("Loading latest sync log entry...");

            var latestLog = await Db.SyncLog
                .Where(l => l.RepositoryId == repo.Id)
                .OrderByDescending(l => l.Started)
                .FirstOrDefaultAsync();

            if (latestLog == null)
            {
                _logger.LogDebug("No latest sync, starting from the beginning of time!");
            }
            else
            {
                if (latestLog.Completed == null)
                {
                    throw new InvalidOperationException($"An outstanding sync is in progress for {repo.Owner}/{repo.Name} (by {latestLog.User}, started at {latestLog.Started})");
                }
                _logger.LogDebug("Syncing new items since {StartTime}", latestLog.Started.ToLocalTime());
            }

            // Determine the start time using the previous water mark
            var lastSyncStart = latestLog?.WaterMark;

            // Set the water mark for this sync to 5 minutes ago (to account for clock drift)
            // We'll likely end up syncing things from before this time, but we'll allow some overlap here.
            var syncStart = DateTimeOffset.UtcNow;
            var waterMark = syncStart.AddMinutes(-5);

            _logger.LogDebug("Setting current water mark to {WaterMark}", waterMark.ToLocalTime());

            // Create a sync log entry
            var nextLog = new SyncLogEntry()
            {
                Repository = repo,
                User = user.Login,
                Started = syncStart,
                WaterMark = waterMark
            };

            // Save to the database
            Db.SyncLog.Add(nextLog);
            await SaveChangesAsync();
            _logger.LogDebug("Sync #{Id} has started...", nextLog.Id);

            return new SyncContext(this, _loggerFactory.CreateLogger<SyncContext>(), repo, user, nextLog, lastSyncStart);
        }
    }
}