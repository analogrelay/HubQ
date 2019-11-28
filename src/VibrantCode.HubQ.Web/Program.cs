using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VibrantCode.HubQ.Web.Data;

namespace VibrantCode.HubQ.Web
{
    public class Program
    {
        private static readonly Assembly Asm = typeof(Program).Assembly;
        public static readonly string Name = Asm.GetName().Name!;
        public static readonly string Version =
            Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
            Asm.GetName().Version!.ToString();

        public static async Task Main(string[] args)
        {
            var stream = typeof(Program).Assembly.GetManifestResourceStream("VibrantCode.HubQ.Web.Data.emoji.json");
            var emojiTable = await EmojiTable.LoadAsync(stream);

            CreateHostBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(emojiTable);
                })
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
