using System;
using System.Diagnostics;
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
            var app = new CommandLineApplication();
            app.Name = Name;
            app.FullName = "HubSync - GitHub Issue Synchronizer";
            app.VersionOption("-v|--version", Version);

            app.Command("sync", cmd =>
            {
                // GitHub connection information
                var tokenOption = cmd.Option("-t|--token <TOKEN>", "The GitHub OAuth Token to use to authenticate", CommandOptionType.SingleValue);

                // Destination connection information
                var sqlConnectionStringOption = cmd.Option("--mssql <CONNECTIONSTRING>", "A Connection String for a Microsoft SQL Server to sync issues to", CommandOptionType.SingleValue);

                // Repositories to sync
                var repositoryArgument = cmd.Argument("<REPOSITORIES...>", "Repositories to sync, in the form [owner]/[repo]", multipleValues: true);

                // Logging options
                var verboseOption = cmd.Option("-v|--verbose", "Be verbose", CommandOptionType.NoValue);
                var agentOption = cmd.Option("-a|--agent-name", "A name for this agent that can identify it in the database", CommandOptionType.SingleValue);

                var process = Process.GetCurrentProcess();
                cmd.OnExecute(() =>
                {
                    // Validate arguments
                    var domain = Environment.UserDomainName ?? Environment.MachineName;
                    var command = new SyncCommand(
                            gitHubCredentials: new Credentials(GetRequiredOption(tokenOption)),
                            sqlConnectionString: GetRequiredOption(sqlConnectionStringOption),
                            repositories: repositoryArgument.Values,
                            agent: GetOptionalOption(agentOption, $"{domain}\\{Environment.UserName} on {Environment.MachineName}, {process.ProcessName}:{process.Id}"),
                            loggerFactory: CreateLogger(verboseOption.HasValue()));

                    return command.ExecuteAsync();
                });
            });

            app.Command("migrate", cmd =>
            {
                // Destination connection information
                var sqlConnectionStringOption = cmd.Option("--mssql <CONNECTIONSTRING>", "A Connection String for a Microsoft SQL Server to sync issues to", CommandOptionType.SingleValue);

                // Logging options
                var verboseOption = cmd.Option("-v|--verbose", "Be verbose", CommandOptionType.NoValue);

                cmd.OnExecute(() =>
                {
                    // Validate arguments
                    var command = new MigrateCommand(
                            sqlConnectionString: GetRequiredOption(sqlConnectionStringOption),
                            loggerFactory: CreateLogger(verboseOption.HasValue()));

                    return command.ExecuteAsync();
                });
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

        private static ILoggerFactory CreateLogger(bool verbose)
        {
            var filters = new LoggerFilterOptions();
            if (!verbose)
            {
                filters.Rules.Add(new LoggerFilterRule(
                    providerName: null,
                    categoryName: null,
                    logLevel: LogLevel.Information,
                    filter: (_, __, ___) => true));
            }

            var factory = new LoggerFactory(new[] {
                new CliConsoleLoggerProvider()
            }, filters);
            return factory;
        }

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
