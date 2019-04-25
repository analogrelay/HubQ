namespace VibrantCode.HubQ.Data
{
    public class IssueAssignee
    {
        public int IssueId { get; set; }
        public int AssigneeId { get; set; }

        public virtual Issue? Issue { get; set; }
        public virtual Actor? Assignee { get; set; }
    }
}
