using System.Collections.Generic;

namespace HubSync.Models
{
    public class Actor: GitHubEntity
    {
        public int Id {get; set;}
        public string? Login {get; set;}
        public string? AvatarUrl { get; set;}

        public virtual IList<Issue>? Issues { get; set; }

        public void UpdateFrom(Octokit.User user)
        {
            // Update the model
            GitHubId = user.Id;
            NodeId = user.NodeId;
            Login = user.Login;
            AvatarUrl = user.AvatarUrl;
        }
    }
}
