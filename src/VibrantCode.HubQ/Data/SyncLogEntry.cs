using System;

namespace VibrantCode.HubQ.Data
{
    public class SyncLogEntry
    {
        public int Id { get; set; }
        public int RepositoryId { get; set; }
        public string? User { get; set; }
        public DateTimeOffset Started { get; set; }
        public DateTimeOffset WaterMark { get; set; }
        public DateTimeOffset? Completed { get; set; }
        public int StartRateLimit { get; set; }
        public int? EndRateLimit { get; set; }

        public virtual Repository? Repository { get; set; }
    }
}
