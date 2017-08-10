using System.Threading.Tasks;
using Octokit;
using System;

namespace HubSync
{
    public abstract class SyncTarget
    {
        public abstract Task<bool> IsSchemaUpToDateAsync();
        public abstract Task<DateTime?> GetLastSyncTimeAsync(string owner, string name);
        public abstract Task RecordStartSyncAsync(string owner, string name, string agent);
        public abstract Task CompleteSyncAsync(string error);
        public abstract Task SyncIssueAsync(Issue issue);
    }
}