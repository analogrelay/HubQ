using System.Collections.Generic;

namespace HubSync.Models
{
    public class Repository
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }

        public virtual IList<SyncHistory> HistoryEntries { get; }
        public virtual IList<Issue> Issues { get; }
    }
}
