using System.Collections.Immutable;
using VibrantCode.HubQ.Web.Client.Components;

namespace VibrantCode.HubQ.Web.Client.State
{
    public class LoadIssuesAction
    {
        public static readonly LoadIssuesAction Instance = new LoadIssuesAction();
        private LoadIssuesAction() { }
    }

    public class LoadedIssuesAction
    {
        public LoadedIssuesAction(ImmutableArray<IssueListItem> issues)
        {
            Issues = issues;
        }

        public ImmutableArray<IssueListItem> Issues { get; }
    }
}
