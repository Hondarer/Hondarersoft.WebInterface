using System;
using WebSocketLibrary;

namespace WebApiServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CommonApiManager commonApiManager = new CommonApiManager();

            commonApiManager.Start();

            Console.ReadLine();
        }
    }
}
