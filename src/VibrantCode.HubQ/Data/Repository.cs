using System.Collections.Generic;

namespace VibrantCode.HubQ.Data
{
    public class Repository : GitHubEntity
    {
        public int Id { get; set; }
        public string? Owner { get; set; }
        public string? Name { get; set; }

        public virtual IList<SyncLogEntry>? LogEntries { get; set; }
        public virtual IList<Issue>? Issues { get; set; }
        public virtual IList<Label>? Labels { get; set; }
        public virtual IList<Milestone>? Milestones { get; set; }

        public void UpdateFrom(Octokit.Repository repo)
        {
            GitHubId = repo.Id;
            NodeId = repo.NodeId;

            Owner = repo.Owner.Login;
            Name = repo.Name;
        }
    }
}
