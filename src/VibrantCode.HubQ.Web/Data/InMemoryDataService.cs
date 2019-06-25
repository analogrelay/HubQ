using System.Collections.Generic;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Data
{
    public class InMemoryDataService : IDataService
    {
        private const string AllRepos = "repo:aspnet/AspNetCore repo:aspnet/Extensions repo:aspnet/AspNetCore-Tooling";
        private const string OpenIssues = "is:issue is:open";

        private static readonly QueueGroup _root = new QueueGroup(
            name: string.Empty,
            children: new QueueGroupEntry[]
            {
                new QueueGroup("SignalR", new QueueGroupEntry[]
                {
                    new QueueDefinition("Triage", TriageQuery("area-signalr"))
                }),
                new QueueGroup("Servers", new QueueGroupEntry[]
                {
                    new QueueDefinition("Triage", TriageQuery("area-servers"))
                }),
                new QueueGroup("Hosting", new QueueGroupEntry[]
                {
                    new QueueDefinition("Triage", TriageQuery("area-hosting"))
                }),
            });

        public Task<QueueGroup> GetRootGroupAsync() => Task.FromResult(_root);

        private static string TriageQuery(string area) => $"{OpenIssues} {AllRepos} label:{area} no:milestone";
    }
}
