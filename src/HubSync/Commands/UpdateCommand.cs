using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync.Commands
{
    [Command("update", Description = "Updates an existing HubSync database.")]
    public class UpdateCommand : DatabaseCommandBase
    {
        private readonly ILogger _logger;

        public UpdateCommand(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateCommand>();
        }

        public async Task<int> OnExecuteAsync()
        {
            var context = CreateDbContext();

            // Get pending migrations
            var migrations = await context.Database.GetPendingMigrationsAsync();

            // Run the migrations
            _logger.LogInformation("Pending migrations");
            foreach (var migration in migrations)
            {
                _logger.LogInformation("* {MigrationName}", migration);
            }
            _logger.LogInformation("Applying all pending migrations...");
            await context.Database.MigrateAsync();

            return 0;
        }
    }
}
