namespace VibrantCode.HubQ.Web.Models
{
    public class IssueResponse
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string? Repository { get; set; }
        public string? Title { get; set; }
    }
}
