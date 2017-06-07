using System;
using Microsoft.Extensions.Logging;

namespace HubSync
{
    internal class CliConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new CliConsoleLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }

    internal class CliConsoleLogger : ILogger
    {
        private static object _consoleLock = new object();

        private readonly string _categoryName;

        public CliConsoleLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return Disposable.Null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Don't run user code in the lock!
            var message = formatter(state, exception);

            lock (_consoleLock)
            {
                var (prefix, fg, bg) = GetLogLevelInfo(logLevel);

                var oldFg = Console.ForegroundColor;
                var oldBg = Console.BackgroundColor;
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;

                Console.Write(prefix);

                Console.ForegroundColor = oldFg;
                Console.BackgroundColor = oldBg;

                Console.WriteLine($": [{_categoryName}] {message}");
            }
        }

        private (string prefix, ConsoleColor foreground, ConsoleColor background) GetLogLevelInfo(LogLevel logLevel)
        {
            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return ("crit", ConsoleColor.White, ConsoleColor.Red);
                case LogLevel.Error:
                    return ("fail", ConsoleColor.Black, ConsoleColor.Red);
                case LogLevel.Warning:
                    return ("warn", ConsoleColor.Yellow, ConsoleColor.Black);
                case LogLevel.Information:
                    return ("info", ConsoleColor.DarkGreen, ConsoleColor.Black);
                case LogLevel.Debug:
                    return ("dbug", ConsoleColor.Gray, ConsoleColor.Black);
                case LogLevel.Trace:
                    return ("trce", ConsoleColor.Gray, ConsoleColor.Black);
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }
    }

    internal class Disposable : IDisposable
    {
        public static readonly IDisposable Null = new Disposable();

        public void Dispose()
        {
        }
    }
}
