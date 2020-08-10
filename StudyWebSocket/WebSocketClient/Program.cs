// C#でWebSocketのサンプルを動かしてみた
// https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (WebSocketLibrary.WebSocketClient webSocketClient = new WebSocketLibrary.WebSocketClient())
            {
                await webSocketClient.ConnectAsync();

                await webSocketClient.SendTextAsync("{\"jsonrpc\": \"2.0\", \"method\": \"subtract\", \"params\": [23, 42], \"id\": 3}");

                Console.ReadLine();
            }
        }
    }
}
