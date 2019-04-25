using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VibrantCode.HubQ.Data
{
    internal class HubSyncContextDesignTimeFactory : IDesignTimeDbContextFactory<HubSyncContext>
    {
        public HubSyncContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlServer(HubSyncContext.LocalConnectionString)
                .Options;
            return new HubSyncContext(options);
        }
    }
}
