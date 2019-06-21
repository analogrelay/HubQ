using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Data
{
    public interface IDataService
    {
        Task<IReadOnlyList<QueueDefinition>> GetQueuesAsync();
    }
}
