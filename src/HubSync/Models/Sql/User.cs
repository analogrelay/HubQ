using System.Collections.Generic;

namespace HubSync.Models.Sql
{
    public class User
    {
        public int Id { get; set; }

        public int GitHubId { get; set; }

        public string Login { get; set; }

        public string AvatarUrl { get; set; }

        public virtual IList<Issue> CreatedIssues { get; set; } = new List<Issue>();

        public virtual IList<Issue> ClosedIssues { get; set; } = new List<Issue>();

        public virtual IList<IssueAssignee> AssignedIssues { get; set; } = new List<IssueAssignee>();
    }
}
