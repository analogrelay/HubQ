using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    [Command("hubsync", Description = "GitHub Issue Synchronizer")]
    [Subcommand(typeof(InitCommand))]
    [Subcommand(typeof(UpdateCommand))]
    [Subcommand(typeof(SyncCommand))]
    class Program
    {
        private enum Verbosity
        {
            Normal,
            Verbose,
            VeryVerbose
        }

        private static readonly Assembly Asm = typeof(Program).Assembly;
        public static readonly string Version = Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        public static readonly string Name = Asm.GetName().Name;

        static int Main(string[] args)
        {
#if DEBUG
            if (args.Any(a => a == "--debug"))
            {
                args = args.Where(a => a != "--debug").ToArray();
                Console.WriteLine($"Ready for debugger to attach. Process ID: {Process.GetCurrentProcess().Id}.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }
#endif

            // Verbose global argument
            var verbose = Verbosity.Normal;
            if (args.Any(a => a == "-vv" || a == "--very-verbose"))
            {
                verbose = Verbosity.VeryVerbose;
                args = args.Where(a => a != "-vv" && a != "--very-verbose").ToArray();
            }
            else if (args.Any(a => a == "-v" || a == "--verbose"))
            {
                verbose = Verbosity.Verbose;
                args = args.Where(a => a != "-v" && a != "--verbose").ToArray();
            }

            var app = new CommandLineApplication<Program>();
            var services = new ServiceCollection();
            ConfigureServices(services, verbose);
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services.BuildServiceProvider());

            app.VersionOption("-v|--version", Version);

            try
            {
                return app.Execute(args);
            }
            catch (CommandLineException clex)
            {
                Console.Error.WriteLine(clex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unhandled exception:");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }

        }

        public void OnExecute(CommandLineApplication app)
        {
            Console.WriteLine("Default");
            app.ShowHelp();
        }

        private static void ConfigureServices(IServiceCollection services, Verbosity verbose)
        {
            services.AddLogging(builder =>
            {
                builder.AddProvider(new CliConsoleLoggerProvider());

                if (verbose != Verbosity.Normal)
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                    if (verbose != Verbosity.VeryVerbose)
                    {
                        builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information);
                    }
                }
            });
        }
    }
}
