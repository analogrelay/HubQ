using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HubSync.Models;
using HubSync.Models.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Octokit;

namespace HubSync
{
    public class MongoDbSyncTarget : SyncTarget
    {
        private Models.Mongo.SyncRecord _currentSync;

        private readonly ILogger<MongoDbSyncTarget> _logger;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Models.Mongo.SyncRecord> _syncHistory;
        private readonly IMongoCollection<Models.Mongo.Issue> _issues;

        private List<WriteModel<Models.Mongo.Issue>> _writes = new List<WriteModel<Models.Mongo.Issue>>();

        public MongoDbSyncTarget(string mongoConnectionString, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MongoDbSyncTarget>();

            var url = new MongoUrl(mongoConnectionString);
            _client = new MongoClient(url);
            _database = _client.GetDatabase(url.DatabaseName);

            _syncHistory = _database.GetCollection<Models.Mongo.SyncRecord>("SyncHistory", new MongoCollectionSettings()
            {
                AssignIdOnInsert = true
            });

            _issues = _database.GetCollection<Models.Mongo.Issue>("Issues", new MongoCollectionSettings()
            {
                AssignIdOnInsert = true
            });
        }

        public override Task<bool> IsSchemaUpToDateAsync()
        {
            _logger.LogInformation("MongoDB is schemaless, so there's nothing to do here.");
            _logger.LogInformation("Use the 'migrate' command to ensure the necessary indexes exist");
            return Task.FromResult(true);
        }

        public override Task SyncIssueAsync(Octokit.Issue issue)
        {
            var filter = Builders<Models.Mongo.Issue>.Filter;

            var issueModel = new Models.Mongo.Issue()
            {
            };

            finish mapping the issueModel

            _writes.Add(new ReplaceOneModel<Models.Mongo.Issue>(
                filter.And(
                    filter.Eq(i => i.RepositoryOwner, issue.Repository.Owner.Login),
                    filter.Eq(i => i.RepositoryName, issue.Repository.Name),
                    filter.Eq(i => i.Number, issue.Number)
                ),
                issueModel));
        }

        public override async Task RecordStartSyncAsync(string owner, string name, string agent)
        {
            if (_currentSync != null)
            {
                throw new InvalidOperationException($"Can't call {nameof(RecordStartSyncAsync)} twice.");
            }

            var syncRecord = new Models.Mongo.SyncRecord()
            {
                RepositoryOwner = owner,
                RepositoryName = name,
                Agent = agent,
                CreatedUtc = DateTime.UtcNow,
                Status = SyncStatus.Synchronizing
            };

            _logger.LogDebug("Inserting Sync Record");
            await _syncHistory.InsertOneAsync(syncRecord);
            _logger.LogDebug("Inserted Sync Record");

            _currentSync = syncRecord;
        }

        public override async Task CompleteSyncAsync(string error)
        {
            if (_currentSync == null)
            {
                throw new InvalidOperationException($"Can't call {nameof(CompleteSyncAsync)} unless {nameof(RecordStartSyncAsync)} has been called");
            }

            // Bulk Write the pending issues
            _logger.LogInformation("Bulk-writing {issueCount} issues", _writes.Count);
            var stopwatch = Stopwatch.StartNew();
            await _issues.BulkWriteAsync(_writes);
            stopwatch.Stop();
            _logger.LogInformation("Bulk-wrote {issueCount} issues in {elapsedMs:0.00}ms", _writes.Count, stopwatch.ElapsedMilliseconds);

            _currentSync.CompletedUtc = DateTime.UtcNow;
            if (string.IsNullOrEmpty(error))
            {
                _currentSync.Status = SyncStatus.Synchronized;
            }
            else
            {
                _currentSync.Status = SyncStatus.Failed;
                _currentSync.Error = error;
            }

            _logger.LogDebug("Inserting Sync Record");
            await _syncHistory.FindOneAndReplaceAsync(
                Builders<Models.Mongo.SyncRecord>.Filter.Eq(r => r.Id, _currentSync.Id),
                _currentSync);
            _logger.LogDebug("Inserted Sync Record");
        }

        public override async Task<DateTime?> GetLastSyncTimeAsync(string owner, string name)
        {
            name = name.ToLowerInvariant();
            owner = owner.ToLowerInvariant();

            _logger.LogDebug("Fetching latest sync record for {owner}/{name}", owner, name);
            var syncRecord = await _syncHistory.Find(r => r.RepositoryOwner == owner && r.RepositoryName == name)
                .SortByDescending(r => r.CreatedUtc)
                .FirstOrDefaultAsync();

            if (syncRecord != null && syncRecord.Status != SyncStatus.Synchronized)
            {
                _logger.LogError("A synchronization is already underway by agent '{agent}'.", syncRecord.Agent);
                _logger.LogError("If you need to forcibly cancel it, run the 'cancel' command.");
                _logger.LogError("NOTE: This will NOT stop active processes associated with the sync.");
                return null;
            }
            return syncRecord?.CreatedUtc ?? DateTime.MinValue;
        }
    }
}