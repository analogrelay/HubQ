using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public class SqlSyncTarget : SyncTarget
    {
        private readonly HubSyncContext _context;
        private readonly ILogger<SqlSyncTarget> _logger;

        private Dictionary<int, User> _userCache = new Dictionary<int, User>();
        private Dictionary<Tuple<int, string>, Label> _labelCache = new Dictionary<Tuple<int, string>, Label>();
        private Dictionary<Tuple<int, int>, Milestone> _milestoneCache = new Dictionary<Tuple<int, int>, Milestone>();

        public SqlSyncTarget(string sqlConnectionString, ILoggerFactory loggerFactory)
        {
            // Set up EF
            var options = new DbContextOptionsBuilder<HubSyncContext>()
                .EnableSensitiveDataLogging(true)
                .UseLoggerFactory(loggerFactory)
                .UseSqlServer(sqlConnectionString)
                .Options;
            _context = new HubSyncContext(options);
            _logger = loggerFactory.CreateLogger<SqlSyncTarget>();
        }

        public override async Task<SyncHistory> GetLastSyncRecordAsync(Repository repo)
        {
            // Find the last successful non-failed status.
            var latest = await _context.SyncHistory
                .Where(h => h.RepositoryId == repo.Id && h.Status != SyncStatus.Failed)
                .OrderByDescending(h => h.CompletedUtc)
                .FirstOrDefaultAsync();
            if (latest != null && latest.Status != SyncStatus.Synchronized)
            {
                _logger.LogError("A synchronization is already underway by agent '{agent}'.", latest.Agent);
                _logger.LogError("If you need to forcibly cancel it, run the 'cancel' command.");
                _logger.LogError("NOTE: This will NOT stop active processes associated with the sync.");
                return null;
            }
            return latest;
        }

        public override async Task<bool> IsSchemaUpToDateAsync()
        {
            // Check for outstanding migrations
            _logger.LogDebug("Checking for pending migrations...");
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                _logger.LogError("Unable to continue, there are pending migrations.");
                foreach (var migration in pendingMigrations)
                {
                    _logger.LogError("Pending Migration: {migrationName}", migration);
                }
                return false;
            }
            _logger.LogDebug("No pending migrations found");
            return true;
        }

        public override async Task<SyncHistory> RecordStartSyncAsync(Repository repo, string agent)
        {
            var syncHistory = new SyncHistory()
            {
                CreatedUtc = DateTime.UtcNow,
                RepositoryId = repo.Id,
                Status = SyncStatus.Synchronizing,
                Agent = agent
            };
            _context.SyncHistory.Add(syncHistory);
            await _context.SaveChangesAsync();
            return syncHistory;
        }

        public override async Task<Repository> GetOrCreateRepoAsync(Octokit.Repository githubRepo)
        {
            var repo = await _context.Repositories
                .FirstOrDefaultAsync(r => r.GitHubId == githubRepo.Id);
            if (repo == null)
            {
                repo = new Repository()
                {
                    GitHubId = githubRepo.Id,
                    Owner = githubRepo.Owner.Login.ToLowerInvariant(),
                    Name = githubRepo.Name.ToLowerInvariant()
                };
                _context.Repositories.Add(repo);
            }
            return repo;
        }
    }
}