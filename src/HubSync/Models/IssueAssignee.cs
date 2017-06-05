namespace HubSync.Models
{
    public class IssueAssignee
    {
        public int IssueId { get; set; }

        public int UserId { get; set; }

        public virtual Issue Issue { get; set; }

        public virtual User User { get; set; }
    }
}
