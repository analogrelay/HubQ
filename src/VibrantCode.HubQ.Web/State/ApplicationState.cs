using System;
using System.Collections.Generic;

namespace VibrantCode.HubQ.Web.State
{
    public class ApplicationState
    {
        // Initial State initialization
        public ApplicationState(string? githubToken)
            : this(githubToken, new QueuesState(isLoading: false, Array.Empty<QueueModel>()))
        {
        }

        public ApplicationState(string? githubToken, QueuesState queues)
        {
            GitHubToken = githubToken;
            Queues = queues;
        }

        public string? GitHubToken { get; }
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
        public IReadOnlyList<QueueModel> Queues { get; }
    }

    public class QueueModel
    {
        public QueueModel(string name, string url, IReadOnlyList<QueueModel> children)
        {
            Name = name;
            Url = url;
            Children = children;
        }

        public string Name { get; }
        public string Url { get; }
        public IReadOnlyList<QueueModel> Children { get; }
    }
}
