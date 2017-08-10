using System;

namespace HubSync.Models.Sql
{
    public class SyncHistory
    {
        public int Id { get; set; }

        public int RepositoryId { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime? CompletedUtc { get; set; }

        public SyncStatus Status { get; set; }

        public string Error { get; set; }

        public string Agent { get; set; }

        public virtual Repository Repository { get; set; }
    }
}
