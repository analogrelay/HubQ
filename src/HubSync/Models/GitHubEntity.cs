namespace HubSync.Models
{
    public abstract class GitHubEntity
    {
        public long GitHubId { get; set; }
        public string? NodeId { get; set; }
    }
}
