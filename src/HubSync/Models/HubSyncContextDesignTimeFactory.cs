using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HubSync.Models
{
    internal class HubSyncContextDesignTimeFactory : IDesignTimeDbContextFactory<HubSyncContext>
    {
        public HubSyncContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlServer(DatabaseCommandBase.LocalConnectionString)
                .Options;
            return new HubSyncContext(options);
        }
    }
}
