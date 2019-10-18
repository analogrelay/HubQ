using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace VibrantCode.HubQ.Web
{
    public class Program
    {
        private static readonly Assembly Asm = typeof(Program).Assembly;
        public static readonly string Name = Asm.GetName().Name!;
        public static readonly string Version =
            Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
            Asm.GetName().Version!.ToString();
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
