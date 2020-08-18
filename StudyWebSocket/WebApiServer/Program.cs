using System;
using WebInterfaceLibrary;

namespace WebApiServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CommonApiManager commonApiManager = new CommonApiManager().Regist(new WebApiService() { AllowCORS = true }).Start();

            Console.ReadLine();
        }
    }
}
