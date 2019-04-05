using System;
using System.Collections.Generic;
using Octokit;

namespace HubSync.Models
{
    public class Actor: GitHubEntity
    {
        public int Id {get; set;}
        public ActorKind Kind {get; set;}
        public string? Name {get; set;}
        public string? AvatarUrl { get; set;}

        public virtual IList<Issue>? Issues { get; set; }
        public virtual IList<IssueAssignee>? IssueAssignments { get; set; }
        public virtual IList<ReviewRequest>? ReviewRequests { get; set; }

        public void UpdateFrom(Octokit.User user)
        {
            // Update the model
            GitHubId = user.Id;
            NodeId = user.NodeId;
            Name = user.Login;
            AvatarUrl = user.AvatarUrl;

            if(Kind == ActorKind.Team)
            {
                throw new InvalidOperationException("Cannot update a team based on a user!");
            }

            Kind = user.Type switch
            {
                AccountType.Bot => ActorKind.Bot,
                AccountType.Organization => ActorKind.Organization,
                AccountType.User => ActorKind.User,
                _ => ActorKind.User
            };
        }
    }
}
