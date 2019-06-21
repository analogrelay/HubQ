using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using VibrantCode.HubQ.Web.Data;
using VibrantCode.HubQ.Web.Nux;

namespace VibrantCode.HubQ.Web.State
{
    public class ApplicationReducer : IReducer<ApplicationState>
    {
        public ApplicationState Reduce(ApplicationState initialState, IAction action, IStoreDispatcher dispatcher)
        {
            return new ApplicationState(
                initialState.Authentication,
                ReduceQueues(initialState.Queues, action, dispatcher));
        }

        private QueuesState ReduceQueues(QueuesState queues, IAction action, IStoreDispatcher dispatcher)
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
