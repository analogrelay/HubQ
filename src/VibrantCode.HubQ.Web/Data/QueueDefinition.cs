using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Data
{
    public class QueueDefinition
    {
        public QueueDefinition(string name, string query)
        {
            Name = name;
            Query = query;
        }

        public string Name { get; }
        public string Query { get; }
    }
}
