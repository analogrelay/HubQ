using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace VibrantCode.HubQ.Web.Client.Api
{
    internal class JsInteropJsonSerializer : IJsonSerializer
    {
        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            var str = await content.ReadAsStringAsync();
            return Json.Deserialize<T>(str);
        }

        public HttpContent SerializeAsync<T>(T obj)
        {
            var str = Json.Serialize(obj);
            var content = new StringContent(str);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }
    }
}
