using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WebApiClient
{
    /// <summary>
    /// REST クライアントの動作確認を行うコマンドを提供します。
    /// </summary>
    class Program
    {
        /// <summary>
        /// プログラムのエントリー ポイントを提供します。
        /// </summary>
        /// <param name="args">プログラムの引数。</param>
        /// <returns>待機する <see cref="Task"/>。</returns>
        static async Task Main(string[] args)
        {
            await new HostBuilder()
            .ConfigureAppConfiguration((hostContext, configBuilder) =>
            {
                // 環境名の組み立て
                string environmentName = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
                if (environmentName == null)
                {
                    environmentName = "production";
                }
                hostContext.HostingEnvironment.EnvironmentName = environmentName;

                // 基底パスの設定
                configBuilder.SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));

                // プログラムの引数を設定
                configBuilder.AddCommandLine(args);

                // 設定ファイルの読込
                string jsonFilePath = $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json";
                if (File.Exists(jsonFilePath) == true)
                {
                    configBuilder.AddJsonFile(jsonFilePath);
                }
            })
            .ConfigureLogging((hostContext, loggingBuilder) =>
            {
                // ログ出力の最低レベルを設定
                loggingBuilder.SetMinimumLevel(LogLevel.Information);

                // Console ロガーの追加
                loggingBuilder.AddConsole(configure =>
                {
                    configure.TimestampFormat = "[HH:mm:ss.fff] ";
                });

#if DEBUG
                // Debug ロガーの追加
                loggingBuilder.AddDebug();
#endif
            })
            .ConfigureServices(services =>
            {
                // サービス処理の紐づけ(AddTransient, AddSingleton)
                services.AddSingleton<IExitService, ExitService>();
                services.AddTransient<IWebApiClient, Hondarersoft.WebInterface.WebApiClient>();
                services.AddSingleton<ICommonApiManager, CommonApiManager>();

                // アプリケーションの実装クラスを指定
                services.AddHostedService<WebApiClientImpl>();
            })
            .RunConsoleAsync();
        }
    }
}
