using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HubSync.Mongo
{
    public class Label
    {
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        public long GitHubId { get; set; }
        public IdRef Repository { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Url { get; set; }
    }
}
