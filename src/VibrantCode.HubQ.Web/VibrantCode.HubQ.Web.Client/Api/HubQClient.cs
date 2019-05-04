using System;
using System.Net.Http;

namespace VibrantCode.HubQ.Web.Client.Api
{
    public class HubQClient
    {
        private readonly HttpClient _client;

        public IssuesClient Issues {get; }

        public HubQClient(string baseUrl, HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri(baseUrl);

            Issues = new IssuesClient(_client, new JsInteropJsonSerializer());
        }
    }
}
