using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.Extensions.Logging;
using Octokit;

namespace HubSync.Synchronization
{
    internal class SyncContext
    {
        private readonly SyncManager _manager;
        private readonly ILogger<SyncContext> _logger;

        public Models.Repository Repo { get; }
        public Actor User { get; }
        public SyncLogEntry LogEntry { get; }
        public DateTimeOffset? Since { get; }

        public SyncContext(SyncManager manager, ILogger<SyncContext> logger, Models.Repository repo, Actor user, SyncLogEntry logEntry, DateTimeOffset? since)
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
                AcceptHeaders.Concat(
                    AcceptHeaders.GitHubAppsPreview,
                    AcceptHeaders.ReactionsPreview));
        }

        public async Task CompleteAsync()
        {
            LogEntry.Completed = DateTimeOffset.UtcNow;
            await _manager.SaveChangesAsync();
        }
    }
}