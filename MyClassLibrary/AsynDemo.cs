using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyClassLibrary
{
    public class AsynDemo
    {
        #region await&async
        private static async Task<string> AsyncInnwer()
        {
            Console.WriteLine("--AsyncInner in threadId:" + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(3000);
            Console.WriteLine("--AsyncInner Done.");
            return "wake up";
        }
        private static async void Async()
        {
            Console.WriteLine("-AsyncWarpper in threadId:" + Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(3000);
            
            string s = await AsyncInnwer();
            Console.WriteLine("- Warpper Done.");
        }
        #endregion

        public static void Run()
        {
            Console.WriteLine("Main in threadId:" + Thread.CurrentThread.ManagedThreadId);
            Async();
            Console.WriteLine("Main Done.");


        }
    }
}
