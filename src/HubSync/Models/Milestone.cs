using System.Collections.Generic;

namespace HubSync.Models
{
    public class Milestone
    {
        public int Id { get; set; }

        public int GitHubId { get; set; }

        public string Title { get; set; }

        public virtual IList<Issue> Issues { get; internal set; }
    }
}
