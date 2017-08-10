using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Octokit;

namespace HubSync
{
    internal class Program
    {
        private static readonly Assembly Asm = typeof(Program).Assembly;
        internal static readonly string Version = Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        internal static readonly string Name = Asm.GetName().Name;

        static int Main(string[] args)
        {
            if (args.Any(a => a == "--debug"))
            {
                args = args.Where(a => a != "--debug").ToArray();
                Console.WriteLine($"Waiting for Debugger to attach. Process ID: {Process.GetCurrentProcess().Id}");
                Console.WriteLine("Press ENTER to continue");
                Console.ReadLine();
            }

            var app = new CommandLineApplication();
            app.Name = Name;
            app.FullName = "HubSync - GitHub Issue Synchronizer";
            app.VersionOption("-v|--version", Version);

            app.Command("sync", cmd =>
            {
                cmd.Description = "Sync changes to issues in the specific repository to the database";

                // GitHub connection information
                var tokenOption = cmd.Option("-t|--token <TOKEN>", "The GitHub OAuth Token to use to authenticate", CommandOptionType.SingleValue);

                // Destination connection information
                var sqlConnectionStringOption = cmd.Option("--mssql <CONNECTIONSTRING>", "A Connection String for a Microsoft SQL Server database to sync issues to", CommandOptionType.SingleValue);
                var mongoConnectionStringOption = cmd.Option("--mongo <CONNECTIONSTRING>", "A Connection String for a MongoDB server to sync issues to (supports Azure Cosmos DB)", CommandOptionType.SingleValue);

                // Repositories to sync
                var repositoryArgument = cmd.Argument("<REPOSITORIES...>", "Repositories to sync, in the form [owner]/[repo]", multipleValues: true);

                var loggingOptions = LoggingOptions.Register(cmd);

                var agentOption = cmd.Option("-a|--agent-name", "A name for this agent that can identify it in the database", CommandOptionType.SingleValue);

                var process = Process.GetCurrentProcess();
                cmd.OnExecute(() =>
                {
                    // Validate arguments
                    var domain = Environment.UserDomainName ?? Environment.MachineName;

                    var loggerFactory = loggingOptions.CreateLoggerFactory();

                    // Determine the target
                    SyncTarget target;
                    if (sqlConnectionStringOption.HasValue() && mongoConnectionStringOption.HasValue())
                    {
                        throw new CommandLineException("Only one of '--mssql' or '--mongo' may be specified.");
                    }
                    else if (sqlConnectionStringOption.HasValue())
                    {
                        target = new SqlSyncTarget(sqlConnectionStringOption.Value(), loggerFactory);
                    }
                    else if (mongoConnectionStringOption.HasValue())
                    {
                        target = new MongoDbSyncTarget(mongoConnectionStringOption.Value(), loggerFactory);
                    }
                    else
                    {
                        throw new CommandLineException("One of '--mssql' or '--mongo' must be specified.")
                    }

                    var command = new SyncCommand(
                            gitHubCredentials: new Credentials(GetRequiredOption(tokenOption)),
                            syncTarget: target,
                            repositories: repositoryArgument.Values,
                            agent: GetOptionalOption(agentOption, $"{domain}\\{Environment.UserName} on {Environment.MachineName}, {process.ProcessName}:{process.Id}"),
                            loggerFactory: loggerFactory);

                    return command.ExecuteAsync();
                });
            });

            app.Command("migrate", cmd =>
            {
                cmd.Description = "Migrate the database to the latest version";

                // Destination connection information
                var sqlConnectionStringOption = cmd.Option("--mssql <CONNECTIONSTRING>", "A Connection String for a Microsoft SQL Server database to migrate.", CommandOptionType.SingleValue);

                var loggingOptions = LoggingOptions.Register(cmd);

                cmd.OnExecute(() =>
                {
                    // Validate arguments
                    var command = new MigrateCommand(
                            sqlConnectionString: GetRequiredOption(sqlConnectionStringOption),
                            loggerFactory: loggingOptions.CreateLoggerFactory());

                    return command.ExecuteAsync();
                });
            });

            app.Command("reset", cmd =>
            {
                cmd.Description = "Reset the database";

                // Destination connection information
                var sqlConnectionStringOption = cmd.Option("--mssql <CONNECTIONSTRING>", "A Connection String for a Microsoft SQL Server database to reset.", CommandOptionType.SingleValue);

                var loggingOptions = LoggingOptions.Register(cmd);

                cmd.OnExecute(() =>
                {
                    // Validate arguments
                    var command = new ResetCommand(
                            sqlConnectionString: GetRequiredOption(sqlConnectionStringOption),
                            loggerFactory: loggingOptions.CreateLoggerFactory());

                    return command.ExecuteAsync();
                });
            });

            app.Command("help", cmd =>
            {
                cmd.Description = "Get help for a specific command";

                var subcommandArgument = cmd.Argument("<COMMAND>", "A command to get help for");

                cmd.OnExecute(() =>
                {
                    app.ShowHelp(subcommandArgument.Value);
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            try
            {
                return app.Execute(args);
            }
            catch (CommandLineException clex)
            {
                app.ShowHelp();

                var oldForeground = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("error");
                Console.ForegroundColor = oldForeground;
                Console.WriteLine($": {clex.Message}");
                return 1;
            }
        }

        private static string GetOptionalOption(CommandOption option, string defaultValue = default(string))
            => option.HasValue() ? option.Value() : defaultValue;

        private static string GetRequiredOption(CommandOption option)
        {
            // Exceptions as control-flow. Yaaaay!
            if (!option.HasValue())
            {
                throw new CommandLineException($"Missing required argument '--{option.LongName}'.");
            }
            return option.Value();
        }
    }
}
