using System.Collections.Generic;

namespace HubSync.Models.Sql
{
    public class Repository
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }

        public virtual IList<SyncHistory> HistoryEntries { get; set; } = new List<SyncHistory>();
        public virtual IList<Issue> Issues { get; set; } = new List<Issue>();
        public virtual IList<Label> Labels { get; set; } = new List<Label>();
        public virtual IList<Milestone> Milestones { get; set; } = new List<Milestone>();
    }
}
