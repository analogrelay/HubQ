using System.Collections.Generic;

namespace HubSync.Models.Sql
{
    public class Label
    {
        public int Id { get; set; }

        public int RepositoryId { get; set; }

        public string Name { get; set; }

        public string Color { get; set; }

        public virtual Repository Repository { get; set; }

        public virtual IList<IssueLabel> Issues { get; set; } = new List<IssueLabel>();
    }
}
