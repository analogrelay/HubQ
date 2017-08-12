using System;
using HubSync.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HubSync.Mongo
{
    public class SyncRecord
    {
        public ObjectId Id { get; set; }
        public IdRef Repository { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        [BsonRepresentation(BsonType.String)]
        public SyncStatus Status { get; set; }
        public string Error { get; set; }
        public string Agent { get; set; }
    }
}
