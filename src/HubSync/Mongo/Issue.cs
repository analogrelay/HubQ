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
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public int CommentCount { get; set; }
        public bool Locked { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public User User { get; set; }
        public User ClosedBy { get; set; }
        public List<User> Assignees { get; set; } = new List<User>();
        public List<Label> Labels { get; set; } = new List<Label>();
        public Milestone Milestone { get; set; }
        public PullRequest PullRequest { get; set; }
        public Reactions Reactions { get; set; }
    }
}
