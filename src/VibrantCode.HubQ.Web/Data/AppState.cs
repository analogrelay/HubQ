using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Octokit;

namespace VibrantCode.HubQ.Web.Data
{
    public class AppState
    {
        public string? GitHubToken
        {
            get => GitHubClient.Credentials?.GetToken();
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    GitHubClient.Credentials = Credentials.Anonymous;
                }
                else
                {
                    GitHubClient.Credentials = new Credentials(value);
                }
            }
        }

        public string? OAuthNonce { get; set; }

        [JsonIgnore]
        public bool IsLoggedIn => !string.IsNullOrEmpty(GitHubToken);

        [JsonIgnore]
        public GitHubClient GitHubClient { get; } = new GitHubClient(
            new ProductHeaderValue(Program.Name, Program.Version));
    }
}
