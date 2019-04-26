using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VibrantCode.HubQ.Data
{
    internal class HubSyncDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HubSyncDbContext>
    {
        public HubSyncDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlServer(HubSyncDbContext.LocalConnectionString)
                .Options;
            return new HubSyncDbContext(options);
        }
    }
}
