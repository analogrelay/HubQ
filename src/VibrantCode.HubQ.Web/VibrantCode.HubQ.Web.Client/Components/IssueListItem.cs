namespace VibrantCode.HubQ.Web.Client.Components
{
    public class IssueListItem
    {
        public IssueListItem(int id, int number, string repository, string title)
        {
            Id = id;
            Number = number;
            Repository = repository;
            Title = title;
        }

        public int Id { get; }
        public int Number { get; }
        public string Repository { get; }
        public string Title { get; }
        public bool Selected { get; set; }
    }
}
