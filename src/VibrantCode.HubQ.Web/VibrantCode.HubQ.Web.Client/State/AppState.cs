using System;

namespace VibrantCode.HubQ.Web.Client.State
{
    public class AppState
    {
        public IssueListState IssueList { get; }

        // Constructs the initial state
        public AppState() : this(new IssueListState()) { }

        public AppState(IssueListState issueList)
        {
            IssueList = issueList;
        }
    }
}
