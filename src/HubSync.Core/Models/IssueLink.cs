namespace HubSync.Models
{
    public class IssueLink
    {
        public int Id { get; set; }
        public int IssueId { get; set; }
        public string? LinkType { get; set; }

        // We can't guarantee the target issue is in the database, so we store the
        // GitHub identity data (repoId, number) so we can look it up later.
        public int RepositoryId { get; set; }
        public int Number { get; set; }

        public virtual Issue? Issue { get; set; }
        public virtual Repository? TargetRepository { get; set; }
    }
}