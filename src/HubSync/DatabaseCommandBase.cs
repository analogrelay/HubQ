using HubSync.Models;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    public abstract class DatabaseCommandBase
    {
        public static readonly string LocalConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=HubSync;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        [Option("--mssql <SQL_CONNECTION_STRING>", Description = "SQL Server connection string.")]
        public string? SqlConnectionString { get; set; }

        [Option("--mssql-local", Description = "Use SQL Server Local Db and the default database name 'HubSync'.")]
        public bool SqlLocalDb { get; set; }
        public ILoggerFactory LoggerFactory { get; }

        protected DatabaseCommandBase(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        internal HubSyncContext CreateDbContext()
        {
            if (SqlLocalDb)
            {
                if (!string.IsNullOrEmpty(SqlConnectionString))
                {
                    throw new CommandLineException("Cannot specify both '--mssql' and '--mssql-local' options.");
                }

                SqlConnectionString = LocalConnectionString;
            }

            if (string.IsNullOrEmpty(SqlConnectionString))
            {
                throw new CommandLineException("Missing required option '--mssql'");
            }

            var options = new DbContextOptionsBuilder()
                .UseSqlServer(SqlConnectionString)
                .UseLoggerFactory(LoggerFactory)
                .Options;
            return new HubSyncContext(options);
        }
    }
}
