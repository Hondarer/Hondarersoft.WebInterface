using System;
using WebSocketLibrary;

namespace WebApiServer
{
    class Program
    {
        static void Main(string[] args)
        {
            WebApiService webApiService = new WebApiService();

            webApiService.Start();

            Console.ReadLine();
        }
    }
}
