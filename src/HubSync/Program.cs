using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    class Program
    {
        private static readonly Assembly Asm = typeof(Program).Assembly;
        private static readonly string Version = Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        private static readonly string Name = Asm.GetName().Name;

        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = Name;
            app.FullName = "HubSync - GitHub Issue Synchronizer";
            app.VersionOption("-v|--version", Version);

            app.Command("sync", cmd =>
            {
                // GitHub connection information
                var userNameOption = cmd.Option("-u|--username <USERNAME>", "The GitHub username to use to authenticate", CommandOptionType.SingleValue);
                var tokenOption = cmd.Option("-t|--token <TOKEN>", "The GitHub OAuth Token to use to authenticate", CommandOptionType.SingleValue);

                // Destination connection information
                var sqlConnectionStringOption = cmd.Option("--mssql <CONNECTIONSTRING>", "A Connection String for a Microsoft SQL Server to sync issues to", CommandOptionType.SingleValue);

                // Repositories to sync
                var repositoryArgument = cmd.Argument("<REPOSITORIES...>", "Repositories to sync, in the form [owner]/[repo]", multipleValues: true);

                // Logging options
                var verboseOption = cmd.Option("-v|--verbose", "Be verbose", CommandOptionType.NoValue);

                cmd.OnExecute(() =>
                {
                    // Validate arguments
                    var command = new SyncCommand(
                            userName: GetRequiredOption(userNameOption),
                            token: GetRequiredOption(tokenOption),
                            sqlConnectionString: GetRequiredOption(sqlConnectionStringOption),
                            repositories: repositoryArgument.Values,
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

        private static ILoggerFactory CreateLogger(bool verbose)
        {
            var factory = new LoggerFactory();
            factory.AddProvider("Console", new CliConsoleLoggerProvider());
            if (!verbose)
            {
                factory.AddFilter((_, __, level) => level >= LogLevel.Information);
            }
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
