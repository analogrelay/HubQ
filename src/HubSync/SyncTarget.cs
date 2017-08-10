using System.Threading.Tasks;
using HubSync.Models;

namespace HubSync
{
    public abstract class SyncTarget
    {
        public abstract Task<bool> IsSchemaUpToDateAsync();
        public abstract Task<SyncHistory> GetLastSyncRecordAsync(Repository repo);
        public abstract Task<SyncHistory> RecordStartSyncAsync(Repository repo, string agent);
        public abstract Task<Repository> GetOrCreateRepoAsync(Octokit.Repository githubRepo);
        public abstract Task SaveChangesAsync();
    }
}