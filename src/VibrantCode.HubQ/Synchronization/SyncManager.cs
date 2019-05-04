using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VibrantCode.HubQ.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Octokit;
using HubSync;

namespace VibrantCode.HubQ.Synchronization
{
    public class SyncManager
    {
        private static readonly IEqualityComparer<(string, string)> _pairwiseIgnoreCaseComparer =
            ValueTupleEqualityComparer.Create(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);

        public HubSyncDbContext Db { get; }
        public GitHubClient GitHub { get; }

        private Dictionary<string, Actor> _actorCache = new Dictionary<string, Actor>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<(string, string), Data.Team> _teamCache = new Dictionary<(string, string), Data.Team>(_pairwiseIgnoreCaseComparer);
        private Dictionary<(string, string), Data.Repository> _repoCache = new Dictionary<(string, string), Data.Repository>(_pairwiseIgnoreCaseComparer);
        private Dictionary<long, Data.Issue> _issueCache = new Dictionary<long, Data.Issue>();
        private Dictionary<string, Data.Label> _labelCache = new Dictionary<string, Data.Label>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Data.Milestone> _milestoneCache = new Dictionary<string, Data.Milestone>(StringComparer.OrdinalIgnoreCase);
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SyncManager> _logger;

        public SyncManager(HubSyncDbContext db, GitHubClient github, ILoggerFactory loggerFactory)
        {
            Db = db;
            GitHub = github;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<SyncManager>();
        }

        public async Task<Data.Team> SyncTeamAsync(Octokit.Team team)
        {
            Data.Team model;
            if (_teamCache.TryGetValue((team.Organization.Name, team.Name), out model))
            {
                _logger.LogTrace("Loaded team {Owner}/{Name} from cache.", team.Organization.Name, team.Name);
            }
            else
            {
                model = await Db.Teams.FirstOrDefaultAsync(t => t.Organization == team.Organization.Name && t.Name == team.Name);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new team {Owner}/{Name}.", team.Organization.Name, team.Name);
                    model = new Data.Team();
                    Db.Actors.Add(model);
                }
            }

            model.UpdateFrom(team);

            _teamCache[(model.Organization!, model.Name!)] = model;
            return model;
        }

        public async Task<Actor> SyncActorAsync(Octokit.User user)
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

        public async Task<Data.Repository> SyncRepoAsync(string owner, string name)
        {
            Data.Repository model;
            if (_repoCache.TryGetValue((owner, name), out model))
            {
                return model;
            }
            else
            {
                Octokit.Repository repo;
                try
                {
                    repo = await GitHub.Repository.Get(owner, name);
                }
                catch (NotFoundException)
                {
                    // Either the repo doesn't exist or we don't have permission. Either way, we will go ahead and create it.
                    model = await Db.Repositories.FirstOrDefaultAsync(r => r.Owner == owner && r.Name == name);
                    if (model == null)
                    {
                        _logger.LogDebug("Repository {Owner}/{Name} does not exist or this user does not have permission for it. Creating a pseudo-repo entry.", owner, name);
                        model = new Data.Repository();
                        Db.Repositories.Add(model);
                    }
                    model.Owner = owner;
                    model.Name = name;
                    _repoCache[(owner, name)] = model;
                    return model;
                }
                return await SyncRepoAsync(repo);
            }
        }

        public async Task<Data.Repository> SyncRepoAsync(Octokit.Repository repo)
        {
            Data.Repository model;
            if (_repoCache.TryGetValue((repo.Owner.Name, repo.Name), out model))
            {
                _logger.LogTrace("Loaded repo {Owner}/{Name} from cache.", model.Owner, model.Name);
            }
            else
            {
                model = await Db.Repositories.FirstOrDefaultAsync(r => r.Owner == repo.Owner.Name && r.Name == repo.Name);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new repo {Owner}/{Name}.", repo.Owner.Name, repo.Name);
                    model = new Data.Repository();
                    Db.Repositories.Add(model);
                }
            }

            model.UpdateFrom(repo);

            _repoCache[(model.Owner!, model.Name!)] = model;
            return model;
        }

        public async Task<Data.Issue> SyncIssueAsync(Data.Repository repo, Octokit.Issue issue)
        {
            Data.Issue model;
            if (_issueCache.TryGetValue(issue.Id, out model))
            {
                _logger.LogTrace("Loaded issue {Owner}/{Name}#{Number} from cache.", repo.Owner, repo.Name, issue.Number);
            }
            else
            {
                model = await Db.Issues
                    .Include(i => i.Assignees!)
                    .Include(i => i.Labels!)
                    .Include(i => i.OutboundLinks!)
                    .FirstOrDefaultAsync(i => i.GitHubId == issue.Id);
                if (model == null)
                {
                    _logger.LogTrace("Synchronizing new issue {Owner}/{Name}#{Number}.", repo.Owner, repo.Name, issue.Number);
                    model = issue.PullRequest != null ?
                        new Data.PullRequest() :
                        new Data.Issue();
                    Db.Issues.Add(model);
                }
            }

            model.UpdateFrom(issue);

            // TODO: Update PR data?

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
            if (model.Assignees == null)
            {
                model.Assignees = new List<IssueAssignee>();
            }
            else
            {
                foreach (var existingAssignee in model.Assignees)
                {
                    Db.IssueAssignees.Remove(existingAssignee);
                }
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
                foreach (var existingLabel in model.Labels)
                {
                    Db.IssueLabels.Remove(existingLabel);
                }
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
                foreach (var existingLink in model.OutboundLinks)
                {
                    Db.IssueLinks.Remove(existingLink);
                }
                model.OutboundLinks.Clear();
            }

            foreach (var parsedLink in ScanForLinks(issue.Body))
            {
                var targetRepo = (string.IsNullOrEmpty(parsedLink.Owner) && string.IsNullOrEmpty(parsedLink.RepoName)) ?
                    repo :
                    await SyncRepoAsync(parsedLink.Owner, parsedLink.RepoName);

                var link = new IssueLink()
                {
                    LinkType = parsedLink.LinkType,
                    TargetRepository = targetRepo,
                    Number = parsedLink.Number,
                };
                Db.IssueLinks.Add(link);
                model.OutboundLinks.Add(link);
            }

            _issueCache[model.GitHubId!.Value] = model;
            return model;
        }

        // I think this regex speaks for itself -nobody ever
        // It's for parsing GitHub issue links in each of the following forms:
        // * '#1234' - In-repo short-form links
        // * 'owner/repo#1234' - Cross-repo short-form links
        // * 'http://github.com/owner/repo/issues/1234/' - URL-based links (also supports HTTPS and "www." prefix as well as the optional trailing '/')
        private static readonly Regex _issueLinkScanner = new Regex(
            @"^(?<linkType>[A-Za-z]+)\s*:?\s+((https?://(www\.)?github.com/(?<owner>[a-zA-Z0-9-_]+)/(?<repo>[a-zA-Z0-9-_]+)/issues/(?<number>[0-9]+)/?)|(((?<owner>[a-zA-Z0-9-_]+ )/(?<repo>[a-zA-Z0-9-_]+))?#(?<number>[0-9]+)))\s*$",
            RegexOptions.Multiline);
        private IEnumerable<ParsedIssueLink> ScanForLinks(string body)
        {
            if (!string.IsNullOrEmpty(body))
            {
                // Ahh, ye olde implementors of IEnumerable (non-generic)
                var matches = _issueLinkScanner.Matches(body).Cast<Match>();
                foreach (var match in matches)
                {
                    // Normalize the link type to lower-case
                    var linkType = match.Groups["linkType"].Value.ToLowerInvariant();
                    var owner = match.Groups["owner"].Value;
                    var repoName = match.Groups["repo"].Value;
                    var number = int.Parse(match.Groups["number"].Value);
                    yield return new ParsedIssueLink(linkType, owner, repoName, number);
                }
            }
        }

        public async Task<Data.Label> SyncLabelAsync(Data.Repository repo, Octokit.Label label)
        {
            Data.Label model;
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
                    model = new Data.Label();
                    Db.Labels.Add(model);
                }
            }

            model.UpdateFrom(label);
            model.Repository = repo;

            _labelCache[label.NodeId] = model;

            return model;
        }

        public async Task<Data.Milestone> SyncMilestoneAsync(Data.Repository repo, Octokit.Milestone milestone)
        {
            Data.Milestone model;
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
                    model = new Data.Milestone();
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

        public async Task<SyncContext> CreateSyncContextAsync(Data.Repository repo, Actor user)
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

            // Get rate limit info
            var rateLimits = await GitHub.Miscellaneous.GetRateLimits();

            // Create a sync log entry
            var nextLog = new SyncLogEntry()
            {
                Repository = repo,
                User = user.Name,
                Started = syncStart,
                WaterMark = waterMark,
                StartRateLimit = rateLimits.Resources.Core.Remaining,
            };

            // Save to the database
            Db.SyncLog.Add(nextLog);
            await SaveChangesAsync();
            _logger.LogDebug("Sync #{Id} has started...", nextLog.Id);

            return new SyncContext(this, _loggerFactory.CreateLogger<SyncContext>(), repo, user, nextLog, lastSyncStart);
        }

        private struct ParsedIssueLink
        {
            public ParsedIssueLink(string linkType, string owner, string repoName, int number)
            {
                LinkType = linkType;
                Owner = owner;
                RepoName = repoName;
                Number = number;
            }

            public string LinkType { get; }
            public string Owner { get; }
            public string RepoName { get; }
            public int Number { get; }
        }
    }
}