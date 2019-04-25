using System.Collections.Generic;
using VibrantCode.HubQ.Web.Client.Components;

namespace VibrantCode.HubQ.Web.Client.Pages
{
    public class IndexViewModel
    {
        public List<IssueListItem> Issues { get; set; } = new List<IssueListItem>()
        {
            new IssueListItem(1, 1234, "aspnet/AspNetCore", "A test issue"),
            new IssueListItem(2, 5678, "aspnet/Extensions", "Another test issue"),
        };

        public IndexViewModel()
        {
        }
    }
}
