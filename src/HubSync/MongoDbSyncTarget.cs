using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public class MongoDbSyncTarget : SyncTarget
    {
        private readonly ILogger<MongoDbSyncTarget> _logger;

        public MongoDbSyncTarget(string mongoConnectionString, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MongoDbSyncTarget>();
        }

        public override Task<bool> IsSchemaUpToDateAsync()
        {
            _logger.LogDebug("MongoDB database is schema-less and doesn't need to be updated");
            return Task.FromResult(true);
        }
    }
}