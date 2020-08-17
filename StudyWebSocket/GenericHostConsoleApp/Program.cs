using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GenericHostConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    // Configの追加
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
                    // サービス処理のDI(AddTransient, AddSingleton)
                    services.AddTransient<ITestService, TestService>();

                    // コンソールアプリケーションの実装クラスを指定
                    services.AddHostedService<GenericHostConsoleAppImpl>();
                })
                .RunConsoleAsync();
        }
    }
}
