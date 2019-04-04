namespace HubSync.Models
{
    public class IssueLabel
    {
        public int LabelId { get; set; }
        public int IssueId { get; set; }

        public virtual Label? Label { get; set; }
        public virtual Issue? Issue { get; set; }
    }
}