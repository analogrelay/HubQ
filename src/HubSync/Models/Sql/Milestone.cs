using System.Collections.Generic;

namespace HubSync.Models.Sql
{
    public class Milestone
    {
        public int Id { get; set; }

        public int RepositoryId { get; set; }

        public int Number { get; set; }

        public string Title { get; set; }

        public virtual Repository Repository { get; internal set; }

        public virtual IList<Issue> Issues { get; internal set; } = new List<Issue>();
    }
}
