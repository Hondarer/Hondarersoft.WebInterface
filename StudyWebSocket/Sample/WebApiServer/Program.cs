using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Hondarersoft.WebInterface;

namespace WebApiServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    // Config の追加
                    hostContext.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "production";
                    configApp.SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
                    configApp.AddCommandLine(args);
                    string jsonFilePath = $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json";
                    if (File.Exists(jsonFilePath) == true)
                    {
                        configApp.AddJsonFile(jsonFilePath);
                    }
                })
                .ConfigureLogging((context, b) =>
                {
                    b.SetMinimumLevel(LogLevel.Information);

                    // Console ロガーの追加
                    b.AddConsole(c =>
                    {
                        c.TimestampFormat = "[HH:mm:ss.fff] ";
                    });
#if DEBUG
                    // Debug ロガーの追加
                    b.AddDebug();
#endif
                })
                .ConfigureServices(services =>
                {
                    // サービス処理の紐づけ(AddTransient, AddSingleton)
                    services.AddSingleton<ICommonApiManager, CommonApiManager>();

                    // コンソールアプリケーションの実装クラスを指定
                    services.AddHostedService<WebApiServerImpl>();
                })
                .RunConsoleAsync();
        }
    }
}
