using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HubSync.Mongo
{
    public class Repository
    {
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        public long GitHubId { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
