using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace VibrantCode.HubQ.SyncTool.Commands
{
    [Command("init", Description = "Initializes a new HubSync database.")]
    public class InitCommand : DatabaseCommandBase
    {
        private readonly ILogger _logger;

        public InitCommand(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InitCommand>();
        }

        public async Task<int> OnExecuteAsync()
        {
            // Recreate the database
            var context = CreateDbContext();

            _logger.LogTrace("Attempting to delete the database, if it already exists...");
            await context.Database.EnsureDeletedAsync();

            // Run the migrations
            _logger.LogInformation("Creating Database...");
            await context.Database.EnsureCreatedAsync();

            return 0;
        }
    }
}
