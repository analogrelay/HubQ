using System.Collections.Generic;

namespace VibrantCode.HubQ.Web.State
{
    public class ApplicationState
    {
        public ApplicationState(AuthenticationState authentication, QueuesState queues)
        {
            Authentication = authentication;
            Queues = queues;
        }

        public AuthenticationState Authentication { get; }
        public QueuesState Queues { get; }
    }

    public class QueuesState
    {
        public QueuesState(bool isLoading, IReadOnlyList<QueueModel> queues)
        {
            IsLoading = isLoading;
            Queues = queues;
        }

        public bool IsLoading { get; }
        public IReadOnlyList<QueueModel> Queues { get;}
    }

    public class QueueModel
    {
        public QueueModel(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class AuthenticationState
    {
        public AuthenticationState(string? gitHubToken)
        {
            GitHubToken = gitHubToken;
        }

        public string? GitHubToken { get; }
    }
}
