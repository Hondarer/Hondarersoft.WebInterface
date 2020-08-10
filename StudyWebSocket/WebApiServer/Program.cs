using System;
using WebSocketLibrary;

namespace WebApiServer
{
    class Program
    {
        static void Main(string[] args)
        {
            WebApiService webApiService = new WebApiService()
            {
                AllowCORS = true
            };

            webApiService.Start();

            Console.ReadLine();
        }
    }
}
