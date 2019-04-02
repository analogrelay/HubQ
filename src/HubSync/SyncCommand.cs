using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HubSync.Models;
using HubSync.Synchronization;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;

namespace HubSync
{
    [Command("sync", Description = "Synchronizes the specified repositories to the database.")]
    public class SyncCommand : DatabaseCommandBase
    {
        [Option("--github <GITHUB_TOKEN>", Description = "GitHub authentication token to use.")]
        public string? GitHubToken { get; set; }

        [Argument(0, "<REPOSITORY...>", "A list of repositories (in 'owner/repo' format) to sync issues for.")]
        public IList<string>? Repositories { get; set; }

        private readonly ILogger<SyncCommand> _logger;

        public SyncCommand(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logger = LoggerFactory.CreateLogger<SyncCommand>();
        }

        public async Task<int> OnExecuteAsync()
        {
            if (string.IsNullOrEmpty(GitHubToken))
            {
                throw new CommandLineException("Missing required parameter '--github'");
            }

            _logger.LogInformation("Checking database status...");

            // Check if the database is there
            var context = CreateDbContext();

            // Check migration status
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                var migrationStr = string.Join(", ", pendingMigrations.Select(m => $"'{m}'"));
                throw new CommandLineException($"There are pending migrations on the database, or it does not exist. Run 'hubsync init' or 'hubsync update' to migrate the database. Pending migrations: {migrationStr}.");
            }

            _logger.LogInformation("Database is ready!");

            var client = new GitHubClient(
                new Connection(
                    new ProductHeaderValue(Program.Name, Program.Version),
                    new LoggingHttpClient(LoggerFactory.CreateLogger<LoggingHttpClient>())));
            client.Credentials = new Credentials(GitHubToken);

            if (Repositories == null || Repositories.Count == 0)
            {
                _logger.LogError("No repositories were specified!");
                return 1;
            }

            var syncManager = new SyncManager(context, client, LoggerFactory);

            var success = true;
            foreach (var repoName in Repositories)
            {
                if (RepositoryReference.TryParse(repoName, out var repoRef))
                {
                    try
                    {
                        success &= await SyncRepoAsync(syncManager, repoRef.Owner, repoRef.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing {Owner}/{Repo}", repoRef.Owner, repoRef.Name);
                        success = false;
                    }
                }
            }

            _logger.LogInformation("HubSync has finished.");
            return success ? 0 : 1;
        }

        private async Task<bool> SyncRepoAsync(SyncManager syncManager, string owner, string name)
        {
            // Get current user info
            var user = await syncManager.GitHub.User.Current();
            var actor = await syncManager.SyncActorAsync(user);

            _logger.LogInformation("Starting issue sync for {Owner}/{Repo} ...", owner, name);

            // Get the repo from GitHub
            var repo = await syncManager.GitHub.Repository.Get(owner, name);
            var repoModel = await syncManager.SyncRepoAsync(repo);

            // Save any changes so far
            await syncManager.SaveChangesAsync();

            var context = await syncManager.CreateSyncContextAsync(repoModel, actor);

            var issues = context.GetIssues();
            IReadOnlyList<Octokit.Issue> issuePage;
            var stopwatch = new Stopwatch();
            while ((issuePage = await issues.NextPageAsync()).Any())
            {
                _logger.LogInformation("Syncing page of {Count} issues...", issuePage.Count);

                foreach(var issue in issuePage)
                {
                    await syncManager.SyncIssueAsync(context.Repo, issue);
                }

                _logger.LogDebug("Saving changes to the database...");
                stopwatch.Restart();
                await syncManager.SaveChangesAsync();
                stopwatch.Stop();
                _logger.LogInformation("Synced {Count} issues in {Elapsed}ms.", issuePage.Count, stopwatch.ElapsedMilliseconds);
                _logger.LogDebug("Current memory usage {FormattedMemoryUsage}", FormatSize(GC.GetTotalMemory(forceFullCollection: false)));
            }

            return true;
        }

        private string FormatSize(long bytes)
        {
            if(bytes / 1024 < 5)
            {
                return $"{bytes} bytes";
            }
            else if (bytes / 1024 / 1024 < 5)
            {
                return $"{((double)bytes) / 1024}KB";
            }
            else
            {
                return $"{((double)bytes) / 1024 / 1024}MB";
            }
        }
    }
}
