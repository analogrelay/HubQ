using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace HubSync.Synchronization
{
    public class Pager<T>
    {
        private static readonly Dictionary<string, string> EmptyDictionary = new Dictionary<string, string>();

        private readonly GitHubClient _client;
        private Uri _url;
        private Dictionary<string, string> _parameters;
        private string _accepts;

        public Pager(GitHubClient client, Uri url, Dictionary<string, string> parameters, string accepts)
        {
            _client = client;
            _url = url;
            _parameters = parameters;
            _accepts = accepts;
        }

        public async Task<IReadOnlyList<T>> NextPageAsync()
        {
            if (_url == null)
            {
                return Array.Empty<T>();
            }

            var resp = await _client.Connection.Get<IReadOnlyList<T>>(_url, _parameters, _accepts);

            if (resp.HttpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Received a {(int)resp.HttpResponse.StatusCode} response from the server.");
            }

            _url = resp.HttpResponse.ApiInfo.GetNextPageUrl();
            _parameters = EmptyDictionary;

            return resp.Body;
        }
    }
}