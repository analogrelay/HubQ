using System.Collections.Generic;

namespace HubSync.Models
{
    public class Label
    {
        public int Id { get; set; }

        public int GitHubId { get; set; }

        public string Name { get; set; }

        public string Color { get; set; }

        public bool Default { get; set; }

        public virtual IList<IssueLabel> Issues { get; set; }
    }
}
