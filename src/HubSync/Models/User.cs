using System.Collections.Generic;

namespace HubSync.Models
{
    public class User
    {
        public int Id { get; set; }

        public int GitHubId { get; set; }

        public string Login { get; set; }

        public string AvatarUrl { get; set; }

        public virtual IList<Issue> CreatedIssues { get; set; }

        public virtual IList<Issue> ClosedIssues { get; set; }

        public virtual IList<IssueAssignee> AssignedIssues { get; set; }
    }
}
