using System.Threading.Tasks;
using System;
using Octokit;

namespace HubSync
{
    public abstract class SyncTarget
    {
        public abstract Task<bool> IsSchemaUpToDateAsync();
        public abstract Task<DateTime?> GetLastSyncTimeAsync(Repository repo);
        public abstract Task RecordStartSyncAsync(Repository repo, string agent);
        public abstract Task CompleteSyncAsync(string error);
        public abstract Task SyncIssueAsync(Repository repo, Issue issue);
        public abstract Task SyncRepositoryAsync(Repository repo);
        public abstract Task SyncMilestoneAsync(Repository repo, Milestone milestone);
        public abstract Task SyncLabelAsync(Repository repo, Label label);
    }
}