namespace VibrantCode.HubQ.Web.Data
{
    public class QueueDefinition: QueueGroupEntry
    {
        public QueueDefinition(string name, string query)
        {
            Name = name;
            Query = query;
        }

        public override string Name { get; }
        public string Query { get; }
    }
}
