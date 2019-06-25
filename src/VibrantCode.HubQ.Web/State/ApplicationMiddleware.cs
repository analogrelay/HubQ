using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VibrantCode.HubQ.Web.Data;
using VibrantCode.HubQ.Web.Nux;

namespace VibrantCode.HubQ.Web.State
{
    public class ApplicationMiddleware : IMiddleware
    {
        private readonly IDataService _data;

        public ApplicationMiddleware(IDataService data)
        {
            _data = data;
        }

        public void Invoke(IStoreDispatcher dispatcher, IAction action, Action<IAction> next)
        {
            // Forward the action
            next(action);

            // Kick off follow-up processes as necessary
            switch (action)
            {
                case Actions.LoadQueues _:
                    _ = LoadQueuesAsync(dispatcher);
                    break;
            }
        }

        private async Task LoadQueuesAsync(IStoreDispatcher dispatcher)
        {
            QueueModel CreateQueueModel(QueueGroupEntry entry, string baseUrl)
            {
                var url = $"{baseUrl}{entry.Name}";
                var children = entry is QueueGroup group
                    ? group.Children.Select(entry => CreateQueueModel(entry, $"{url}/")).ToList()
                    : (IReadOnlyList<QueueModel>)Array.Empty<QueueModel>();
                return new QueueModel(entry.Name, url, children);
            }

            var queues = await _data.GetRootGroupAsync();
            var models = queues.Children.Select((entry) => CreateQueueModel(entry, string.Empty)).ToList();
            dispatcher.Dispatch(new Actions.LoadedQueues(models));
        }
    }
}
