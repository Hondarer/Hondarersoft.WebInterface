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
        static void Main(string[] args)
        {
            Run().Wait();
        }

        static async Task Run()
        {
            //クライアント側のWebSocketを定義
            ClientWebSocket ws = new ClientWebSocket();

            //接続先エンドポイントを指定
            Uri uri = new Uri("ws://localhost:8000/ws/");

            //サーバに対し、接続を開始
            await ws.ConnectAsync(uri, CancellationToken.None);
            byte[] buffer = new byte[1024];

            //情報取得待ちループ
            while (true)
            {
                //所得情報確保用の配列を準備
                ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

                //サーバからのレスポンス情報を取得
                WebSocketReceiveResult result = await ws.ReceiveAsync(segment, CancellationToken.None);

                //エンドポイントCloseの場合、処理を中断
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK",
                      CancellationToken.None);
                    return;
                }

                //バイナリの場合は、当処理では扱えないため、処理を中断
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.InvalidMessageType,
                      "I don't do binary", CancellationToken.None);
                    return;
                }

                //メッセージの最後まで取得
                int count = result.Count;
                while (!result.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                          "That's too long", CancellationToken.None);
                        return;
                    }
                    segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                    result = await ws.ReceiveAsync(segment, CancellationToken.None);

                    count += result.Count;
                }

                //メッセージを取得
                string message = Encoding.UTF8.GetString(buffer, 0, count);
                Console.WriteLine("> " + message);
            }
        }
    }
}
