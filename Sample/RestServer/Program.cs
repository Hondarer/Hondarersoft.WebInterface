using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Hondarersoft.WebInterface;
using Hondarersoft.Hosting;
using Microsoft.Extensions.Configuration.Json;
using System.Text.Json;
using System.Reflection;
using Hondarersoft.WebInterface.Swagger;

namespace RestServer
{
    /// <summary>
    /// REST サーバーの動作確認を行うコマンドを提供します。
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
            IConfiguration configuration = null;

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

                string settingsSubName = null;
                if (environmentName.ToLower() != "production")
                {
                    settingsSubName = environmentName.ToLower() + ".";
                }

                // 基底パスの設定
                configBuilder.SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));

                // プログラムの引数を設定
                configBuilder.AddCommandLine(args);

                // 設定ファイルの読込
                string jsonFilePath = $"{Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))}.appsettings.{settingsSubName}json";
                if (File.Exists(jsonFilePath) == true)
                {
                    configBuilder.AddJsonFile(jsonFilePath);
                }

                configuration = configBuilder.Build();
            })
            .ConfigureLogging((hostContext, loggingBuilder) =>
            {
                // ログ出力の最低レベルを設定
                loggingBuilder.SetMinimumLevel(LogLevel.Information);

                // Console ロガーの追加
                loggingBuilder.AddSimpleConsole(configure =>
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
                // 基本のサービス処理の紐づけ(AddTransient, AddSingleton)
                services.AddSingleton<IExitService, ExitService>();
                services.AddTransient<IHttpService, HttpService>();
                services.AddTransient<ISwaggerServerService, SwaggerServerService>();
                services.AddSingleton<ICommonApiService, CommonApiService>();

                // コントローラーの動作に必要となる追加のサービス処理の紐づけ
                // (コントローラー自体を動的に読み込むようにしているため、
                //  コントローラーが依存する追加サービスも動的に読み込めるようにしている)
                services.AddServiceFromConfigration(configuration);

                // アプリケーションの実装クラスを指定
                services.AddHostedService<RestServerImpl>();
            })
            .RunConsoleAsync();
        }
    }
}
