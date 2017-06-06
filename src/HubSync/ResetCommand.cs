using System;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    internal class ResetCommand
    {
        private readonly string _sqlConnectionString;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ResetCommand> _logger;

        public ResetCommand(string sqlConnectionString, ILoggerFactory loggerFactory)
        {
            _sqlConnectionString = sqlConnectionString;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ResetCommand>();
        }

        public async Task<int> ExecuteAsync()
        {
            var options = new DbContextOptionsBuilder<HubSyncContext>()
                .UseSqlServer(_sqlConnectionString)
                .Options;
            var context = new HubSyncContext(options);

            _logger.LogInformation("Deleting existing database.");

            await context.Database.EnsureDeletedAsync();

            _logger.LogInformation("Migrating database.");

            await context.Database.MigrateAsync();

            _logger.LogInformation("Reset complete.");

            return 0;
        }
    }
}
