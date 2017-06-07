using System;
using System.Collections.Generic;

namespace HubSync.Models
{
    public class Issue
    {
        public int Id { get; set; }

        public int RepositoryId { get; set; }

        public int Number { get; set; }

        public int GitHubId { get; set; }

        public int? MilestoneId { get; set; }

        public int? ClosedById { get; set; }

        public int? UserId { get; set; }

        public bool Locked { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ClosedAt { get; set; }

        public string PullRequestUrl { get; set; }

        public int CommentCount { get; set; }

        public string Body { get; set; }

        public string Title { get; set; }

        public IssueState State { get; set; }

        public string HtmlUrl { get; set; }

        public string Url { get; set; }

        public bool IsPr { get; set; }

        public PullRequest PullRequest { get; set; }

        public virtual User User { get; set; }

        public virtual User ClosedBy { get; set; }

        public virtual Repository Repository { get; set; }

        public virtual Milestone Milestone { get; set; }

        public virtual IList<IssueAssignee> Assignees { get; set; } = new List<IssueAssignee>();

        public virtual IList<IssueLabel> Labels { get; set; } = new List<IssueLabel>();
    }
}
