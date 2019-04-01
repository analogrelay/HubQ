using System;
using System.Collections.Generic;
using Octokit;

namespace HubSync.Models
{
    public class Issue : GitHubEntity
    {
        public int Id { get; set; }
        public int RepositoryId { get; set; }
        public int Number { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public ItemState? State { get; set; }
        public bool Locked { get; set; }
        public int CommentCount { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public IssueReactions? Reactions { get; set; }
        public int AuthorId { get; set; }
        public int? MilestoneId { get; set; }

        public virtual Milestone? Milestone { get; set; }
        public virtual Actor? Author { get; set; }
        public virtual Repository? Repository { get; set; }
        public virtual IList<IssueAssignee>? Assignees { get; set; }
        public virtual IList<IssueLabel>? Labels { get; set; }

        internal void UpdateFrom(Octokit.Issue issue)
        {
            GitHubId = issue.Id;
            NodeId = issue.NodeId;

            Number = issue.Number;
            Title = issue.Title;
            Body = issue.Body;
            State = issue.State.Value;
            Locked = issue.Locked;
            CommentCount = issue.Comments;
            ClosedAt = issue.ClosedAt;
            CreatedAt = issue.CreatedAt;
            UpdatedAt = issue.UpdatedAt;

            Reactions = new IssueReactions(
                issue.Reactions.TotalCount,
                issue.Reactions.Plus1,
                issue.Reactions.Minus1,
                issue.Reactions.Confused,
                issue.Reactions.Heart,
                issue.Reactions.Hooray,
                issue.Reactions.Laugh);
        }
    }

    public class IssueReactions
    {
        // Properties must have a private setter for EF to see them.
        public int TotalCount { get; private set; }
        public int Plus1 { get; private set; }
        public int Minus1 { get; private set; }
        public int Confused { get; private set; }
        public int Heart { get; private set; }
        public int Hooray { get; private set; }
        public int Laugh { get; private set; }

        public IssueReactions(int totalCount, int plus1, int minus1, int confused, int heart, int hooray, int laugh)
        {
            TotalCount = totalCount;
            Plus1 = plus1;
            Minus1 = minus1;
            Confused = confused;
            Heart = heart;
            Hooray = hooray;
            Laugh = laugh;
        }
    }
}
