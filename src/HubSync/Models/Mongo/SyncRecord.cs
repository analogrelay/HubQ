using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HubSync.Models.Mongo
{
    public class SyncRecord
    {
        public ObjectId Id { get; set; }

        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime? CompletedUtc { get; set; }

        [BsonRepresentation(BsonType.String)]
        public SyncStatus Status { get; set; }

        public string Error { get; set; }

        public string Agent { get; set; }
    }
}
