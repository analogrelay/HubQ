using MongoDB.Bson;

namespace HubSync.Mongo
{
    public class User
    {
        public int GitHubId { get; set; }
        public string Login { get; set; }
        public string Url { get; set; }
    }
}