namespace HubSync.Models
{
    public class IssueLink
    {
        public int Id { get; set; }
        public int IssueId { get; set; }
        public string? LinkType { get; set; }

        // We can't guarantee the target issue is in the database, so we store the
        // GitHub identity data (owner, name, number) so we can look it up later.
        public string? RepoOwner { get; set; }
        public string? RepoName { get; set; }
        public int Number { get; set; }

        // This can be null if the issue is from a different repo or the link hasn't been fully resolved yet.
        public int? TargetId { get; set; }

        public virtual Issue? Issue { get; set; }
        public virtual Issue? Target { get; set; }
    }
}