using System;

namespace VibrantCode.HubQ.Data
{
    public class Team : Actor
    {
        public string? Organization { get; set; }

        public void UpdateFrom(Octokit.Team team)
        {
            // Update the model
            GitHubId = team.Id;
            NodeId = team.NodeId;
            Name = team.Name;
            Organization = team.Organization.Name;

            if(Kind != ActorKind.Team)
            {
                throw new InvalidOperationException("Cannot update a user based on a team!");
            }
        }
    }
}
