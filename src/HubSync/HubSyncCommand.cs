using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public class HubSyncCommand
    {
        private readonly string _userName;
        private readonly string _token;
        private readonly string _sqlConnectionString;
        private readonly IList<string> _repositories;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<HubSyncCommand> _logger;

        public HubSyncCommand(string userName, string token, string sqlConnectionString, IList<string> repositories, ILoggerFactory loggerFactory)
        {
            _userName = userName;
            _token = token;
            _sqlConnectionString = sqlConnectionString;
            _repositories = repositories;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<HubSyncCommand>();
            if (repositories.Count == 0)
            {
                throw new CommandLineException("At least one repository must be specified");
            }
        }

        public Task<int> ExecuteAsync()
        {
            _logger.LogInformation("HubSync is starting...");

            _logger.LogInformation("HubSync has finished.");
            return Task.FromResult(0);
        }
    }
}
