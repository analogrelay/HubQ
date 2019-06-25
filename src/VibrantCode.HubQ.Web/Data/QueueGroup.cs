using System.Collections.Generic;

namespace VibrantCode.HubQ.Web.Data
{
    public class QueueGroup: QueueGroupEntry
    {
        public QueueGroup(string name, IReadOnlyList<QueueGroupEntry> children)
        {
            Name = name;
            Children = children;
        }

        public override string Name { get; }
        public IReadOnlyList<QueueGroupEntry> Children { get; }
    }
}
