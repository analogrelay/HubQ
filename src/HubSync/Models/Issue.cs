using System;
using Octokit;

namespace HubSync.Models
{
    public class Issue : GitHubEntity
    {
        public int Id { get; set; }
        public int RepositoryId { get; set; }
        public int Number { get; set; }
        public string? Title { get; set; }
        public int AuthorId { get; set; }

        public virtual Actor? Author { get; set; }
        public virtual Repository? Repository { get; set; }

        internal void UpdateFrom(Octokit.Issue issue)
        {
            GitHubId = issue.Id;
            NodeId = issue.NodeId;

            Number = issue.Number;
            Title = issue.Title;
        }
    }
}
