using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HubSync.Models;
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

            var success = true;
            foreach (var repoName in Repositories)
            {
                if (RepositoryReference.TryParse(repoName, out var repoRef))
                {
                    try
                    {
                        success &= await SyncRepoAsync(client, context, repoRef);
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

        private async Task<bool> SyncRepoAsync(GitHubClient github, HubSyncContext db, RepositoryReference repoRef)
        {
            // Get current user info
            var user = await github.User.Current();

            _logger.LogInformation("Starting issue sync for {Owner}/{Repo} ...", repoRef.Owner, repoRef.Name);

            // Get the latest sync log
            _logger.LogDebug("Loading latest sync log entry...");
            var latestLog = await db.SyncLog
                .Where(l => l.Owner == repoRef.Owner && l.Repository == repoRef.Name)
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
                    _logger.LogError("An outstanding sync is in progress for {Owner}/{Repo} (by {User}, started at {Started})",
                        latestLog.Owner, latestLog.Repository, latestLog.User, latestLog.Started);
                    return false;
                }
                _logger.LogDebug("Syncing new items since {StartTime}", latestLog.Started.ToLocalTime());
            }

            // Determine the start time using the previous water mark
            var lastSyncStart = latestLog?.WaterMark;

            // Set the water mark for this sync to 5 minutes ago (to account for clock drift)
            var syncStart = DateTimeOffset.UtcNow;
            var waterMark = syncStart.AddMinutes(-5);
            _logger.LogDebug("Setting current water mark to {WaterMark}", waterMark.ToLocalTime());

            // Create a sync log entry
            var nextLog = new SyncLogEntry()
            {
                Owner = repoRef.Owner,
                Repository = repoRef.Name,
                User = user.Login,
                Started = syncStart,
                WaterMark = waterMark
            };

            // Save to the database
            db.SyncLog.Add(nextLog);
            await db.SaveChangesAsync();
            _logger.LogDebug("Sync #{Id} has started...", nextLog.Id);

            // Get the issues since the last sync
            var request = new RepositoryIssueRequest()
            {
                Since = lastSyncStart,
                State = ItemStateFilter.All
            };
            var options = new ApiOptions()
            {
                PageCount = 1,
                PageSize = 100,
                StartPage = 0
            };
            _logger.LogInformation("Retrieving page of issues...");
            var url = new Uri($"https://api.github.com/repos/{repoRef.Owner}/{repoRef.Name}/issues");
            var parameters = new Dictionary<string, string>()
            {
                { "per_page", "100" }
            };
            if (lastSyncStart != null)
            {
                parameters["since"] = lastSyncStart.Value.UtcDateTime.ToString("O");
            }
            var accepts = AcceptHeaders.GitHubAppsPreview;
            while (url != null)
            {
                var resp = await github.Connection.Get<IReadOnlyList<Issue>>(url, parameters, accepts);
                if (resp.HttpResponse.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("GitHub returned a {StatusCode} error.", resp.HttpResponse.StatusCode);
                    return false;
                }

                var apiInfo = resp.HttpResponse.ApiInfo;
                _logger.LogInformation("Syncing page of {Count} issues...", resp.Body.Count);
                foreach (var issue in resp.Body)
                {
                    //_logger.LogDebug("Syncing #{Number} {Title} (by @{Author})", issue.Number, issue.Title, issue.User.Login);
                }

                url = apiInfo.GetNextPageUrl();
            }

            return true;
        }
    }
}
