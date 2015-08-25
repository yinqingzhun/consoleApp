using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleApp;
using Topshelf;

namespace TopShelfDemo
{
    public class SendMsgService
    {
        Thread t = null;
        public void Start()
        {
            int i = 1;
            t = new Thread(() =>
             {
                 try
                 {
                     Array array = Enum.GetValues(typeof(MqQueueName));
                     Random r = new Random();
                     while (true)
                     {
                         MqQueueName name = (MqQueueName)array.GetValue(r.Next(array.Length));
                         MqRepository.Instance.SendMessage(name, i++.ToString());
                         Thread.Sleep(5000);
                     }
                 }
                 catch (Exception ex)
                 {
                     LogHelper.Fatal("线程退出", ex);
                 }

             });
            t.Start();

        }

        public void Stop()
        {
            t.Abort();
        }
    }
}
