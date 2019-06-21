using System.Collections.Generic;
using VibrantCode.HubQ.Web.Nux;

namespace VibrantCode.HubQ.Web.State
{
    public static class Actions
    {
        public class LoadQueues: IAction
        {
            public static IAction Instance = new LoadQueues();
            private LoadQueues() { }
        }

        public class LoadedQueues : IAction
        {
            public LoadedQueues(IReadOnlyList<QueueModel> queues)
            {
                Queues = queues;
            }

            public IReadOnlyList<QueueModel> Queues { get; }
        }
    }
}
