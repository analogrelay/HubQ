
using System;
using System.Collections.Generic;

namespace HubSync.Mongo
{
    public class PullRequest
    {
        public long GitHubId { get; set; }
        public string Url { get; set; }
        public bool Merged { get; set; }
        public DateTime? MergedAtUtc { get; set; }
        public User MergedBy { get; set; }
        public IList<User> RequestedReviewers { get; set; }
    }
}