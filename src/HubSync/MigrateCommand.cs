using System;
using System.Linq;
using System.Threading.Tasks;
using HubSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public class MigrateCommand
    {
        private readonly string _sqlConnectionString;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<MigrateCommand> _logger;

        public MigrateCommand(string sqlConnectionString, ILoggerFactory loggerFactory)
        {
            _sqlConnectionString = sqlConnectionString;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<MigrateCommand>();
        }

        public async Task<int> ExecuteAsync()
        {
            _logger.LogInformation("Running database migrations...");

            var options = new DbContextOptionsBuilder<HubSyncContext>()
                .UseLoggerFactory(_loggerFactory)
                .UseSqlServer(_sqlConnectionString)
                .Options;
            var context = new HubSyncContext(options);

            await context.Database.MigrateAsync();

            _logger.LogInformation("Migration complete.");

            return 0;
        }
    }
}
