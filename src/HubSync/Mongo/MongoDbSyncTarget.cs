using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Octokit;

namespace HubSync.Mongo
{
    public class MongoDbSyncTarget : SyncTarget
    {
        private Mongo.SyncRecord _currentSync;

        private readonly ILogger<MongoDbSyncTarget> _logger;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;

        private readonly IMongoCollection<Mongo.Issue> _issues;
        private readonly IMongoCollection<Mongo.Label> _labels;
        private readonly IMongoCollection<Mongo.Milestone> _milestones;
        private readonly IMongoCollection<Mongo.SyncRecord> _syncHistory;
        private readonly IMongoCollection<Mongo.Repository> _repositories;

        private Dictionary<long, Mongo.Label> _labelCache = new Dictionary<long, Mongo.Label>();
        private Dictionary<long, Mongo.Milestone> _milestoneCache = new Dictionary<long, Mongo.Milestone>();
        private Dictionary<long, Mongo.Repository> _repoCache = new Dictionary<long, Mongo.Repository>();

        private List<WriteModel<Mongo.Issue>> _issuesToAdd = new List<WriteModel<Mongo.Issue>>();

        public MongoDbSyncTarget(string mongoConnectionString, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MongoDbSyncTarget>();

            var url = new MongoUrl(mongoConnectionString);
            _client = new MongoClient(url);
            _database = _client.GetDatabase(url.DatabaseName);

            var collectionSettings = new MongoCollectionSettings()
            {
                AssignIdOnInsert = true
            };

            var conventions = new ConventionPack();
            conventions.Add(new CamelCaseElementNameConvention());
            ConventionRegistry.Register("HubSync", conventions, t => true);

            _issues = _database.GetCollection<Mongo.Issue>("issues", collectionSettings);
            _labels = _database.GetCollection<Mongo.Label>("labels", collectionSettings);
            _milestones = _database.GetCollection<Mongo.Milestone>("milestones", collectionSettings);
            _syncHistory = _database.GetCollection<Mongo.SyncRecord>("syncHistory", collectionSettings);
            _repositories = _database.GetCollection<Mongo.Repository>("repositories", collectionSettings);
        }

        public override Task<bool> IsSchemaUpToDateAsync()
        {
            _logger.LogInformation("MongoDB is schemaless, so there's nothing to do here.");
            _logger.LogInformation("Use the 'migrate' command to ensure the necessary indexes exist");
            return Task.FromResult(true);
        }

        public override async Task SyncIssueAsync(Octokit.Repository githubRepo, Octokit.Issue githubIssue)
        {
            var repo = await GetRepositoryAsync(githubRepo);
            if (repo == null)
            {
                throw new InvalidOperationException($"Cannot add issue #{githubIssue.Number}, it appears to be from a different repository!");
            }

            var filter = Builders<Mongo.Issue>.Filter;

            var issue = new Mongo.Issue()
            {
                GitHubId = githubIssue.Id,
                Repository = new IdRef() { Ref = repo.Id },
                Url = githubIssue.Url,
                Number = githubIssue.Number,
                Title = githubIssue.Title,
                State = githubIssue.State == ItemState.Open ? IssueState.Open : IssueState.Closed,
                Body = githubIssue.Body,
                User = CreateUser(githubIssue.User),
                CreatedAtUtc = githubIssue.CreatedAt.UtcDateTime,
                ClosedBy = CreateUser(githubIssue.ClosedBy),
                ClosedAtUtc = githubIssue.ClosedAt?.UtcDateTime,
                CommentCount = githubIssue.Comments,
                Locked = githubIssue.Locked,
                UpdatedAtUtc = githubIssue.UpdatedAt?.UtcDateTime,
                Assignees = githubIssue.Assignees.Select(CreateUser).ToList(),
                PullRequest = CreatePullRequest(githubIssue.PullRequest),
                Reactions = CreateReactions(githubIssue.Reactions),
            };

            foreach (var githubLabel in githubIssue.Labels)
            {
                var label = await GetLabelAsync(githubLabel);
                if (label == null)
                {
                    _logger.LogWarning("The label {labelName}, applied to {repoOwner}/{repoName}#{issueNumber} was applied mid-sync, a resync will be needed to update this.", githubLabel.Name, repo.Owner, repo.Name, issue.Number);
                }
                else
                {
                    issue.Labels.Add(new IdRef() { Ref = label.Id });
                }
            }

            if (githubIssue.Milestone != null)
            {
                var milestone = await GetMilestoneAsync(githubIssue.Milestone);
                if (milestone == null)
                {
                    _logger.LogWarning("The milestone {milestoneTitle}, applied to {repoOwner}/{repoName}#{issueNumber} was applied mid-sync, a resync will be needed to update this.", githubIssue.Milestone.Title, repo.Owner, repo.Name, issue.Number);
                }
                else
                {
                    issue.Milestone = new IdRef() { Ref = milestone.Id };
                }
            }

            var replace = new ReplaceOneModel<Mongo.Issue>(
                filter.Eq(i => i.GitHubId, githubIssue.Id),
                issue);
            replace.IsUpsert = true;

            _issuesToAdd.Add(replace);
        }

        private Mongo.PullRequest CreatePullRequest(Octokit.PullRequest pullRequest)
        {
            return pullRequest == null ? null : new Mongo.PullRequest()
            {
                GitHubId = pullRequest.Id,
                Url = pullRequest.Url,
                Merged = pullRequest.Merged,
                MergedAtUtc = pullRequest.MergedAt?.UtcDateTime,
                MergedBy = CreateUser(pullRequest.MergedBy),
                RequestedReviewers = pullRequest.RequestedReviewers.Select(CreateUser).ToList(),
            };
        }

        private Mongo.Reactions CreateReactions(ReactionSummary reactions)
        {
            return reactions == null ? null : new Mongo.Reactions()
            {
                Confused = reactions.Confused,
                Heart = reactions.Heart,
                Hooray = reactions.Hooray,
                Laugh = reactions.Laugh,
                Minus1 = reactions.Minus1,
                Plus1 = reactions.Plus1,
                TotalCount = reactions.TotalCount
            };
        }

        private Mongo.User CreateUser(Octokit.User user)
        {
            return user == null ? null : new Mongo.User()
            {
                GitHubId = user.Id,
                Login = user.Login,
                Url = user.Url,
            };
        }

        public override async Task SyncRepositoryAsync(Octokit.Repository githubRepo)
        {
            var repo = await GetRepositoryAsync(githubRepo);

            var isNew = false;
            if (repo == null)
            {
                repo = new Mongo.Repository();
                isNew = true;
            }

            repo.GitHubId = githubRepo.Id;
            repo.Owner = githubRepo.Owner.Login;
            repo.Name = githubRepo.Name;
            repo.Url = githubRepo.Url;

            if (isNew)
            {
                await _repositories.InsertOneAsync(repo);
            }
            else
            {
                await _repositories.ReplaceOneAsync(
                    Builders<Repository>.Filter.Eq(r => r.Id, repo.Id),
                    repo);
            }

            _repoCache[githubRepo.Id] = repo;
        }

        public override async Task SyncMilestoneAsync(Octokit.Repository githubRepo, Octokit.Milestone githubMilestone)
        {
            var repo = await GetRepositoryAsync(githubRepo);
            if (repo == null)
            {
                throw new InvalidOperationException("Cannot add milestone {milestoneId}, it appears to be from a different repository!");
            }
            var milestone = await GetMilestoneAsync(githubMilestone);

            var isNew = false;
            if (milestone == null)
            {
                milestone = new Mongo.Milestone();
                milestone.Repository = new IdRef();
                isNew = true;
            }

            milestone.Repository.Ref = repo.Id;
            milestone.GitHubId = githubMilestone.Id;
            milestone.Number = githubMilestone.Number;
            milestone.Title = githubMilestone.Title;
            milestone.Url = githubMilestone.Url;

            if (isNew)
            {
                await _milestones.InsertOneAsync(milestone);
            }
            else
            {
                await _milestones.ReplaceOneAsync(
                    Builders<Milestone>.Filter.Eq(r => r.Id, milestone.Id),
                    milestone);
            }

            _milestoneCache[githubMilestone.Id] = milestone;
        }

        public override async Task SyncLabelAsync(Octokit.Repository githubRepo, Octokit.Label githubLabel)
        {
            var repo = await GetRepositoryAsync(githubRepo);
            if (repo == null)
            {
                throw new InvalidOperationException("Cannot add label {labelId}, it appears to be from a different repository!");
            }
            var label = await GetLabelAsync(githubLabel);

            var isNew = false;
            if (label == null)
            {
                label = new Mongo.Label();
                label.Repository = new IdRef();
                isNew = true;
            }

            label.Name = githubLabel.Name;
            label.Repository.Ref = repo.Id;
            label.GitHubId = githubLabel.Id;
            label.Color = githubLabel.Color;
            label.Url = githubLabel.Url;

            if (isNew)
            {
                await _labels.InsertOneAsync(label);
            }
            else
            {
                await _labels.ReplaceOneAsync(
                    Builders<Label>.Filter.Eq(r => r.Id, label.Id),
                    label);
            }

            _labelCache[githubLabel.Id] = label;
        }

        public override async Task RecordStartSyncAsync(Octokit.Repository githubRepo, string agent)
        {
            var repo = await GetRepositoryAsync(githubRepo);

            if (_currentSync != null)
            {
                throw new InvalidOperationException($"Can't call {nameof(RecordStartSyncAsync)} twice.");
            }

            var syncRecord = new Mongo.SyncRecord()
            {
                Repository = new IdRef()
                {
                    Ref = repo.Id
                },
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

            if (string.IsNullOrEmpty(error))
            {
                try
                {
                    // Bulk Write the pending issues
                    if (_issuesToAdd.Count > 0)
                    {
                        _logger.LogInformation("Bulk-writing {issueCount} issues", _issuesToAdd.Count);
                        var stopwatch = Stopwatch.StartNew();
                        var result = await _issues.BulkWriteAsync(_issuesToAdd);
                        stopwatch.Stop();
                        _logger.LogInformation("Bulk-write of {count} issues complete in {elapsedMs:0.00}ms", result.RequestCount, stopwatch.ElapsedMilliseconds);
                        _logger.LogInformation("Matched {matches} documents, inserted {inserts}, updated {updates}", result.MatchedCount, result.RequestCount - result.ModifiedCount, result.ModifiedCount);
                    }
                    else
                    {
                        _logger.LogInformation("No issues to add!");
                    }
                }
                catch (Exception ex)
                {
                    // Record the error
                    error = ex.ToString();
                }
            }

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
                Builders<Mongo.SyncRecord>.Filter.Eq(r => r.Id, _currentSync.Id),
                _currentSync);
            _logger.LogDebug("Inserted Sync Record");
        }

        public override async Task<DateTime?> GetLastSyncTimeAsync(Octokit.Repository githubRepo)
        {
            var repo = await GetRepositoryAsync(githubRepo);

            _logger.LogDebug("Fetching latest sync record for {owner}/{name}", repo.Owner, repo.Name);
            var syncRecord = await _syncHistory.Find(r => r.Repository.Ref == repo.Id && r.Status != SyncStatus.Failed)
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

        private async Task<Mongo.Label> GetLabelAsync(Octokit.Label githubLabel)
        {
            if (githubLabel == null)
            {
                return null;
            }

            if (_labelCache.TryGetValue(githubLabel.Id, out var label))
            {
                return label;
            }

            return await _labels.Find(f => f.GitHubId == githubLabel.Id).FirstOrDefaultAsync();
        }

        private async Task<Mongo.Milestone> GetMilestoneAsync(Octokit.Milestone githubMilestone)
        {
            if (githubMilestone == null)
            {
                return null;
            }

            if (_milestoneCache.TryGetValue(githubMilestone.Id, out var milestone))
            {
                return milestone;
            }

            return await _milestones.Find(f => f.GitHubId == githubMilestone.Id).FirstOrDefaultAsync();
        }

        private async Task<Mongo.Repository> GetRepositoryAsync(Octokit.Repository githubRepo)
        {
            if (githubRepo == null)
            {
                return null;
            }

            if (_repoCache.TryGetValue(githubRepo.Id, out var repo))
            {
                return repo;
            }

            return await _repositories.Find(f => f.GitHubId == githubRepo.Id).FirstOrDefaultAsync();
        }
    }
}