// C#でWebSocketのサンプルを動かしてみた
// https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebInterfaceLibrary.Schemas;

namespace WebSocketClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (WebInterfaceLibrary.WebSocketClient webSocketClient = new WebInterfaceLibrary.WebSocketClient())
            {
                await webSocketClient.ConnectAsync();

                // 統一した要求の形式を設けて、そこに要求したい

                await webSocketClient.SendJsonAsync(new JsonRpcRequest() { Method = "cpumodes.localhost.get" });

                // 戻っては来ているが、同期して受け取る処理をまだ書いていない

                Console.ReadLine();
            }
        }
    }
}
