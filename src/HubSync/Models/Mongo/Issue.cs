using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HubSync.Models.Mongo
{
    public class Issue
    {
        public ObjectId Id { get; set; }
        public string RepositoryName { get; set; }
        public string RepositoryOwner { get; set; }
        public int Number { get; set; }
    }
}
