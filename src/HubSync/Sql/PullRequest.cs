using System;

namespace HubSync.Sql
{
    public class PullRequest
    {
        public bool IsPr { get; set; }
        public string BaseSha { get; set; }
        public string BaseRef { get; set; }
        public bool Merged { get; }
        public bool? Mergeable { get; set; }
        public string HeadSha { get; set; }
        public string HeadRef { get; set; }
        public DateTimeOffset? MergedAt { get; set; }
        public int ChangedFiles { get; set; }
        public int? MergedById { get; set; }
        public virtual User MergedBy { get; set; }
    }
}
