using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VibrantCode.HubQ.Data;
using Microsoft.Extensions.Logging;
using Octokit;

namespace VibrantCode.HubQ.Synchronization
{
    public class SyncContext
    {
        private static readonly string _acceptHeaders = AcceptHeaders.Concat(AcceptHeaders.GitHubAppsPreview, AcceptHeaders.ReactionsPreview);
        private readonly SyncManager _manager;
        private readonly ILogger<SyncContext> _logger;

        public Data.Repository Repo { get; }
        public Actor User { get; }
        public SyncLogEntry LogEntry { get; }
        public DateTimeOffset? Since { get; }

        public SyncContext(SyncManager manager, ILogger<SyncContext> logger, Data.Repository repo, Actor user, SyncLogEntry logEntry, DateTimeOffset? since)
        {
            _manager = manager;
            _logger = logger;
            Repo = repo;
            User = user;
            LogEntry = logEntry;
            Since = since;
        }

        public Pager<Octokit.Issue> GetIssues()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "state", "all" },
                { "per_page", "100" }
            };
            if (Since is DateTimeOffset since)
            {
                parameters["since"] = since.UtcDateTime.ToString("O");
            }
            return new Pager<Octokit.Issue>(
                _manager.GitHub,
                new Uri($"https://api.github.com/repos/{Repo.Owner}/{Repo.Name}/issues"),
                parameters,
                _acceptHeaders);
        }

        public async Task CompleteAsync()
        {
            LogEntry.EndRateLimit = _manager.GitHub.GetLastApiInfo().RateLimit.Remaining;
            LogEntry.Completed = DateTimeOffset.UtcNow;
            await _manager.SaveChangesAsync();
        }

        public Pager<Octokit.Milestone> GetMilestones()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "state", "all" },
                { "per_page", "100" }
            };
            // No 'since' field but that's OK because milestones are fewer
            return new Pager<Octokit.Milestone>(
                _manager.GitHub,
                new Uri($"https://api.github.com/repos/{Repo.Owner}/{Repo.Name}/milestones"),
                parameters,
                _acceptHeaders);
        }

        public Pager<Octokit.Label> GetLabels()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "per_page", "100" }
            };
            // No 'since' field but that's OK because milestones are fewer
            return new Pager<Octokit.Label>(
                _manager.GitHub,
                new Uri($"https://api.github.com/repos/{Repo.Owner}/{Repo.Name}/labels"),
                parameters,
                _acceptHeaders);
        }
    }
}