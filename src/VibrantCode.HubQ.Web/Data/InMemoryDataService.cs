using System.Collections.Generic;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Data
{
    public class InMemoryDataService : IDataService
    {
        private const string AllRepos = "repo:aspnet/AspNetCore repo:aspnet/Extensions repo:aspnet/AspNetCore-Tooling";
        private const string OpenIssues = "is:issue is:open";

        private static readonly IReadOnlyList<QueueDefinition> _queues = new[]
        {
            new QueueDefinition("SignalR/Triage", TriageQuery("area-signalr")),
            new QueueDefinition("Servers/Triage", TriageQuery("area-servers")),
            new QueueDefinition("Hosting/Triage", TriageQuery("area-hosting")),
        };

        public Task<IReadOnlyList<QueueDefinition>> GetQueuesAsync() => Task.FromResult(_queues);


        private static string TriageQuery(string area) => $"{OpenIssues} {AllRepos} label:{area}";
    }
}
