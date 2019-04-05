namespace HubSync.Models
{
    public class ReviewRequest
    {
        public int Id { get; set; }
        public int PullRequestId { get; set; }
        public int UserId { get; set; }

        public virtual PullRequest? PullRequest { get; set; }
        public virtual Actor? User { get; set; }
    }
}