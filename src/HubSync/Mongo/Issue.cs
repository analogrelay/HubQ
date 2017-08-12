using System;
using System.Collections.Generic;
using HubSync.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HubSync.Mongo
{
    public class Issue
    {
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        public long GitHubId { get; set; }
        public IdRef Repository { get; set; }
        public string Url { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public IssueState State { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ClosedAtUtc { get; set; }
        public int CommentCount { get; set; }
        public bool Locked { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public User User { get; set; }
        public User ClosedBy { get; set; }
        public List<User> Assignees { get; set; } = new List<User>();
        public List<IdRef> Labels { get; set; } = new List<IdRef>();
        public IdRef Milestone { get; set; }
        public PullRequest PullRequest { get; set; }
        public Reactions Reactions { get; set; }
    }
}
