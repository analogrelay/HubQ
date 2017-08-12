using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HubSync.Mongo
{
    public class Milestone
    {
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        public long GitHubId { get; set; }
        public IdRef Repository { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
