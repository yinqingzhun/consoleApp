using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TopShelfDemo
{
    public class MyClass
    {
        readonly Timer _timer;

        private static readonly string FileName = AppDomain.CurrentDomain.BaseDirectory+ @"\" + "test.txt";

        public MyClass()
        {
            _timer = new Timer(5000)
            {
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += delegate(object sender, ElapsedEventArgs e)
            {
                this.write(string.Format("Run DateTime {0}", DateTime.Now));
            };
        }

        void write(string context)
        {
            StreamWriter sw = File.AppendText(FileName);
            sw.WriteLine(context);
            sw.Flush();
            sw.Close();
        }

        public void Start()
        {
            this.write(string.Format("Start DateTime {0}", DateTime.Now));
        }

        public void Stop()
        {
            this.write(string.Format("Stop DateTime {0}", DateTime.Now) + Environment.NewLine);
        }

    }
}
