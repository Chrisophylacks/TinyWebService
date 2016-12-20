using System;
using System.Collections.Generic;
using System.Threading;

namespace TinyWebService.Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestProbing();
            //MultiServiceTest.Run();
            //CallbacksTest.Run();
            //NestedTest.Run();
            //MultiReturnTest.Run();
            DowncastTest.Run();
            //OverloadsTest.Run();
            Console.ReadLine();
        }

        static void TestProbing()
        {
            try
            {
                TinyClient.Create<IServer>("test");
            }
            catch (TinyWebServiceException)
            {
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
