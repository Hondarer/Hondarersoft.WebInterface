// C#でWebSocketのサンプルを動かしてみた
// https://qiita.com/Zumwalt/items/53797b0156ebbdcdbfb1

using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        static async Task Run()
        {
            //Httpリスナーを立ち上げ、クライアントからの接続を待つ
            HttpListener s = new HttpListener();
            s.Prefixes.Add("http://localhost:8000/ws/");
            s.Start();
            HttpListenerContext hc = await s.GetContextAsync();

            //クライアントからのリクエストがWebSocketでない場合は処理を中断
            if (!hc.Request.IsWebSocketRequest)
            {
                //クライアント側にエラー(400)を返却し接続を閉じる
                hc.Response.StatusCode = 400;
                hc.Response.Close();
                return;
            }

            //WebSocketでレスポンスを返却
            HttpListenerWebSocketContext wsc = await hc.AcceptWebSocketAsync(null);
            WebSocket ws = wsc.WebSocket;

            //１０回のレスポンスを返却
            for (int i = 0; i != 10; ++i)
            {
                //1回のレスポンスごとに2秒のウエイトを設定
                await Task.Delay(2000);

                //レスポンスのテストメッセージとして、現在時刻の文字列を取得
                string time = DateTime.Now.ToLongTimeString();

                //文字列をByte型に変換
                byte[] buffer = Encoding.UTF8.GetBytes(time);
                ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

                //クライアント側に文字列を送信
                await ws.SendAsync(segment, WebSocketMessageType.Text,
                  true, CancellationToken.None);
            }

            //接続を閉じる
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
              "Done", CancellationToken.None);
        }
    }
}
