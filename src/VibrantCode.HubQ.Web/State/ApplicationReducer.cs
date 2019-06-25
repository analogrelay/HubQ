using System;
using VibrantCode.HubQ.Web.Nux;

namespace VibrantCode.HubQ.Web.State
{
    public class ApplicationReducer : IReducer<ApplicationState>
    {
        public ApplicationState Reduce(ApplicationState initialState, IAction action)
        {
            return new ApplicationState(
                initialState.GitHubToken,
                ReduceQueues(initialState.Queues, action));
        }

        private QueuesState ReduceQueues(QueuesState queues, IAction action)
        {
            return action switch
            {
                Actions.LoadQueues _ => new QueuesState(isLoading: true, Array.Empty<QueueModel>()),
                Actions.LoadedQueues lq => new QueuesState(isLoading: false, lq.Queues),
                _ => queues,
            };
        }
    }
}
