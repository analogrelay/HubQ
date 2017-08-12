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

        private readonly ApiOptions apiOptions = new ApiOptions()
        {
            PageSize = 100
        };

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
            var repo = await _github.Repository.Get(owner, name);

            // Ensure the repository is synced
            await _syncTarget.SyncRepositoryAsync(repo);

            // Get the last sync time for issues
            var syncTime = await _syncTarget.GetLastSyncTimeAsync(repo);
            if (syncTime == null)
            {
                // Cancel, because a sync is already in progress
                return false;
            }

            // Create the new sync history entry
            await _syncTarget.RecordStartSyncAsync(repo, _agent);

            try
            {
                // Sync Labels and Milestones first
                await SyncLabelsAsync(repo);
                await SyncMilestonesAsync(repo);

                // Sync Issues changed since the last sync
                await SyncIssuesAsync(repo, syncTime.Value);

                // Update sync history
                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("Saving issues to database...");
                await _syncTarget.CompleteSyncAsync(error: null);
                stopwatch.Stop();
                _logger.LogInformation("Saved issues in {elapsedMs:0.00}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                // Record the error
                await _syncTarget.CompleteSyncAsync(error: ex.ToString());
                throw;
            }

            return true;
        }

        private async Task SyncMilestonesAsync(Repository repo)
        {
            _logger.LogInformation("Fetching milestones for {repoOwner}/{repoName} ...", repo.Owner.Login, repo.Name);
            var milestones = await _github.Issue.Milestone.GetAllForRepository(repo.Id);
            var apiInfo = _github.GetLastApiInfo();
            _logger.LogInformation("Fetched {count} milestones for {repoOwner}/{repoName}. Rate Limit remaining: {remainingRateLimit}", milestones.Count, repo.Owner.Login, repo.Name, apiInfo.RateLimit.Remaining);

            foreach (var milestone in milestones)
            {
                await _syncTarget.SyncMilestoneAsync(repo, milestone);
            }

            _logger.LogInformation("Synced {count} milestones.", milestones.Count);
        }

        private async Task SyncLabelsAsync(Repository repo)
        {
            _logger.LogInformation("Fetching labels for {repoOwner}/{repoName} ...", repo.Owner.Login, repo.Name);
            var labels = await _github.Issue.Labels.GetAllForRepository(repo.Id);
            var apiInfo = _github.GetLastApiInfo();
            _logger.LogInformation("Fetched {count} labels for {repoOwner}/{repoName}. Rate Limit remaining: {remainingRateLimit}", labels.Count, repo.Owner.Login, repo.Name, apiInfo.RateLimit.Remaining);

            foreach (var label in labels)
            {
                await _syncTarget.SyncLabelAsync(repo, label);
            }

            _logger.LogInformation("Synced {count} labels.", labels.Count);
        }

        private async Task SyncIssuesAsync(Repository repo, DateTime? syncTime)
        {
            _logger.LogInformation("Fetching issues for {repoOwner}/{repoName} changed since {syncTime} ...", repo.Owner.Login, repo.Name, syncTime == null ? "the beginning of time" : syncTime.ToString());
            var issues = await _github.Issue.GetAllForRepository(repo.Id, CreateIssueRequest(syncTime), apiOptions);
            var apiInfo = _github.GetLastApiInfo();
            _logger.LogInformation("Fetched {issueCount} issues for {repoOwner}/{repoName}. Rate Limit remaining: {remainingRateLimit}", issues.Count, repo.Owner.Login, repo.Name, apiInfo.RateLimit.Remaining);

            foreach (var issue in issues)
            {
                await _syncTarget.SyncIssueAsync(repo, issue);
            }

            _logger.LogInformation("Synced {count} issues.", issues.Count);
        }

        private RepositoryIssueRequest CreateIssueRequest(DateTime? syncTime)
        {
            var request = new RepositoryIssueRequest()
            {
                State = Octokit.ItemStateFilter.All
            };

            if (syncTime != null)
            {
                request.Since = new DateTimeOffset(syncTime.Value, TimeSpan.Zero);
            }
            return request;
        }
    }
}
