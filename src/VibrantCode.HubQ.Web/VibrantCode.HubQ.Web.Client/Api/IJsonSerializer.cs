using System.Net.Http;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Client.Api
{
    internal interface IJsonSerializer
    {
        Task<T> DeserializeAsync<T>(HttpContent content);
        HttpContent SerializeAsync<T>(T obj);
    }
}