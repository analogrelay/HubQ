using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;

namespace HubSync
{
    public class SyncCommand
    {
        private readonly SyncTarget _syncTarget;
        private readonly IReadOnlyList<string> _repositories;
        private readonly string _agent;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SyncCommand> _logger;
        private readonly IGitHubClient _github;

        public SyncCommand(Octokit.Credentials gitHubCredentials, SyncTarget syncTarget, IReadOnlyList<string> repositories, string agent, ILoggerFactory loggerFactory)
        {
            _syncTarget = syncTarget;
            _repositories = repositories;
            _agent = agent;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<SyncCommand>();

            // Set up GitHub
            _github = new GitHubClient(new ProductHeaderValue("HubSync", Program.Version));
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

            // Get the sync history entry for this repo
            var splat = repoName.Split('/');
            if (splat.Length != 2)
            {
                _logger.LogError("Invalid repository name: {repoName}", repoName);
                return false;
            }
            var owner = splat[0];
            var name = splat[1];
            var syncTime = await _syncTarget.GetLastSyncTimeAsync(owner, name);
            if (syncTime == null)
            {
                // Cancel, because a sync is already in progress
                return false;
            }

            // Create the new sync history entry
            await _syncTarget.RecordStartSyncAsync(owner, name, _agent);

            try
            {
                // Fetch the data
                var apiOptions = new ApiOptions()
                {
                    PageSize = 100
                };

                var issues = await _github.Issue.GetAllForRepository(owner, name, CreateIssueRequest(syncTime.Value), apiOptions);

                // Grab API info (for rate limit impact)
                var apiInfo = _github.GetLastApiInfo();
                _logger.LogInformation("Fetched {issueCount} issues for '{repo}'. Rate Limit remaining: {remainingRateLimit}", issues.Count, $"{owner}/{name}", apiInfo.RateLimit.Remaining);

                await IngestIssuesAsync(issues);

                _logger.LogInformation("Synced {issueCount} issues.", issues.Count);

                // Update sync history
                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("Saving issues to database...");
                await _syncTarget.CompleteSyncAsync(error: null);
                stopwatch.Stop();
                _logger.LogInformation("Saved {issueCount} issues in {elapsedMs:0.00}ms", issues.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                // Record the error
                await _syncTarget.CompleteSyncAsync(error: ex.ToString());
                throw;
            }

            return true;
        }

        private async Task IngestIssuesAsync(IReadOnlyList<Issue> issues)
        {
            foreach (var issue in issues)
            {
                await _syncTarget.SyncIssueAsync(issue);
            }
        }

        private RepositoryIssueRequest CreateIssueRequest(DateTime syncTime)
        {
            var request = new RepositoryIssueRequest()
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
