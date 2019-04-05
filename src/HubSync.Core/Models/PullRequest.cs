using System;
using System.Collections.Generic;

namespace HubSync.Models
{
    public class PullRequest: Issue
    {
        public DateTimeOffset? MergedAt {get; set;}
        public string? MergeCommitSha {get; set;}
        public bool? Draft {get; set;}
        public CommitInfo? Head {get; set;}
        public CommitInfo? Base {get; set;}

        public virtual IList<ReviewRequest>? ReviewRequests {get; set;}
    }
}
