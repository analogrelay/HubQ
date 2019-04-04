using System.Collections.Generic;

namespace HubSync.Models
{
    public class Label : GraphQlEntity
    {
        public int Id { get; set; }
        public int RepositoryId { get; set; }
        public string? Name { get; set; }
        public string? Color { get; set; }
        public string? Description { get; set; }

        public virtual Repository? Repository { get; set; }
        public virtual IList<IssueLabel>? Issues { get; set; }

        public void UpdateFrom(Octokit.Label label)
        {
            NodeId = label.NodeId;
            Name = label.Name;
            Color = label.Color;
            Description = label.Description;
        }
    }
}
