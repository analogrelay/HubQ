using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

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

            // GitHub connection information
            var userNameOption = app.Option("-u|--username <USERNAME>", "The GitHub username to use to authenticate", CommandOptionType.SingleValue);
            var tokenOption = app.Option("-t|--token <TOKEN>", "The GitHub OAuth Token to use to authenticate", CommandOptionType.SingleValue);

            // Destination connection information
            var sqlConnectionStringOption = app.Option("--mssql <CONNECTIONSTRING>", "A Connection String for a Microsoft SQL Server to sync issues to", CommandOptionType.SingleValue);

            // Repositories to sync
            var repositoryArgument = app.Argument("<REPOSITORIES...>", "Repositories to sync, in the form [owner]/[repo]", multipleValues: true);

            app.OnExecute(() =>
            {
                // Validate arguments
                var userName = GetRequiredOption(userNameOption);
                var token = GetRequiredOption(tokenOption);
                var sqlConnectionString = GetRequiredOption(sqlConnectionStringOption);

                var repositories = repositoryArgument.Values;

                if (repositories.Count == 0)
                {
                    throw new CommandLineException("At least one repository must be specified");
                }

                return ExecuteAsync(userName, token, sqlConnectionString, repositories);
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

        private static Task<int> ExecuteAsync(string userName, string token, string sqlConnectionString, List<string> repositories)
        {
            throw new NotImplementedException();
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
