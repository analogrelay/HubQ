using System;
using System.Collections.Generic;

namespace VibrantCode.HubQ.Data
{
    public class Milestone : GraphQlEntity
    {
        public int Id {get; set;}
        public int RepositoryId {get; set;}
        public int Number {get; set;}
        public string? Title {get; set;}
        public DateTimeOffset? DueOn {get; set;}
        public Octokit.ItemState State { get; set;}

        public virtual Repository? Repository { get; set; }
        public virtual IList<Issue>? Issues {get; set;}

        public void UpdateFrom(Octokit.Milestone milestone)
        {
            NodeId = milestone.NodeId;
            Number = milestone.Number;
            Title = milestone.Title;
            DueOn = milestone.DueOn;
            State = milestone.State.Value;
        }
    }
}
