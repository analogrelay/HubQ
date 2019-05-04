using System.Collections.Immutable;
using VibrantCode.HubQ.Web.Client.Components;

namespace VibrantCode.HubQ.Web.Client.State
{
    public class IssueListState
    {
        public IssueListState() : this(false, false, ImmutableArray.Create<IssueListItem>()) { }

        public IssueListState(bool loaded, bool loading, ImmutableArray<IssueListItem> issues)
        {
            Loaded = loaded;
            Loading = loading;
            Issues = issues;
        }

        public bool Loading { get; }
        public bool Loaded { get; }
        public ImmutableArray<IssueListItem> Issues { get; }
    }
}