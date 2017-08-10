using System;

namespace HubSync.Models
{
    public class SyncRecord
    {
        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime? CompletedUtc { get; set; }

        public SyncStatus Status { get; set; }

        public string Error { get; set; }

        public string Agent { get; set; }
    }
}
