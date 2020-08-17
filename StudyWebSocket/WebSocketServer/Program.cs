// C#でWebSocketのサンプルを動かしてみた
// https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1

// [C#]System.Net.WebSocketsを試す。その２。サーバー編。
// http://kimux.net/?p=956

using System;
using System.Threading.Tasks;

namespace WebSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebInterfaceLibrary.WebSocketService webSocketService = new WebInterfaceLibrary.WebSocketService())
            {
                webSocketService.Start();

                Console.ReadKey();
            }
        }
    }
}
