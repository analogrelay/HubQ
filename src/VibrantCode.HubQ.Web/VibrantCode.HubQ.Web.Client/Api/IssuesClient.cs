using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using VibrantCode.HubQ.Web.Models;

namespace VibrantCode.HubQ.Web.Client.Api
{
    public class IssuesClient
    {
        private HttpClient _client;
        private readonly IJsonSerializer _serializer;

        internal IssuesClient(HttpClient client, IJsonSerializer serializer)
        {
            _client = client;
            _serializer = serializer;
        }

        public async Task<IEnumerable<IssueResponse>> GetAllAsync(int? pageNumber, int? pageSize)
        {
            var resp = await _client.GetAsync(UrlUtils.GetUrl("/api/issues", new { pageNumber, pageSize }));
            resp.EnsureSuccessStatusCode();
            return await _serializer.DeserializeAsync<IssueResponse[]>(resp.Content);
        }
    }
}