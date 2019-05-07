using System.Collections.Immutable;
using System.Threading.Tasks;
using Nux;
using VibrantCode.HubQ.Web.Client.Components;

namespace VibrantCode.HubQ.Web.Client.State
{
    public class AppReducer : Reducer<AppState>
    {
        protected override AppState Reduce(AppState current, object action, IStore<AppState> store)
        {
            switch (action)
            {
                case LoadIssuesAction _:
                    _ = LoadIssuesAsync(store);
                    return new AppState(new IssueListState(loaded: false, loading: true, current.IssueList.Issues));
                case LoadedIssuesAction loaded:
                    return new AppState(new IssueListState(loaded: true, loading: false, loaded.Issues));
            }

            return current;
        }

        private async Task LoadIssuesAsync(IStore<AppState> store)
        {
            await Task.Delay(2000);
            var issues = ImmutableArray.CreateBuilder<IssueListItem>();
            issues.Add(new IssueListItem(1, 1234, "aspnet/AspNetCore", "An issue"));
            issues.Add(new IssueListItem(2, 5678, "aspnet/Extensions", "Another issue"));
            store.Dispatch(new LoadedIssuesAction(issues.ToImmutable()));
        }
    }
}
