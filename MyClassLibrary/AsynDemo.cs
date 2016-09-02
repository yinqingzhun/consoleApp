using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClassLibrary
{
    public class AsynDemo
    {
        #region await&async
        private static async Task<string> Async2()
        {
            await Task.Delay(3000);
            return "wake up";
        }
        private static async void Async()
        {
            Console.WriteLine("loading...");
            string s = await Async2();
            Console.WriteLine(s);
        }
        #endregion

        public static void Run()
        {
            Async();
        }
    }
}
