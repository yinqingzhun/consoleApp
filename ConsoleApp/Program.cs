using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.SqlServer.Server;

using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using MyClassLibrary;


namespace ConsoleApp
{

    class Program
    {
        class CustomData
        {
            public int Name { get; set; }
            public long CreationTime { get; set; }
            public int ThreadNum { get; set; }
        }
        public static void Main()
        {

            try
            {

                //readMsg();
                //writeMsg();
                //HandleMqMsg();
                var tokenSource2 = new CancellationTokenSource();
                CancellationToken ct = tokenSource2.Token;

                var task = Task.Factory.StartNew(() =>
                {

                    // Were we already canceled?
                    ct.ThrowIfCancellationRequested();

                    bool moreToDo = true;
                    while (moreToDo)
                    {
                        // Poll on this property if you have to do
                        // other cleanup before throwing.
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            ct.ThrowIfCancellationRequested();
                        }

                    }
                }, tokenSource2.Token); // Pass same token to StartNew.

                tokenSource2.Cancel();

                // Just continue on this thread, or Wait/WaitAll with try-catch:
                try
                {
                    task.Wait();
                }
                catch (AggregateException e)
                {
                    foreach (var v in e.InnerExceptions)
                        Console.WriteLine(e.Message + " " + v.Message);
                }

                Console.ReadKey();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }
        private static void HandleMqMsg()
        {
            Action<string> a = p =>
            {
                Console.WriteLine(p);
                Thread.Sleep(1000);
            };
            MqRepository.Instance.ReceiveMessageBySharingMode(MqQueueName.Attention, a);
            MqRepository.Instance.ReceiveMessageBySharingMode(MqQueueName.ClubPost, a);
            MqRepository.Instance.ReceiveMessageBySharingMode(MqQueueName.ClubTopic, a);
            MqRepository.Instance.ReceiveMessageBySharingMode(MqQueueName.ForumTopic, a);

        }
        class NoName
        {
            [DefaultValueAttribute("cpu")]
            public string CPU { get; set; }
            public string PSU { get; set; }
            public string[] Drives { get; set; }

        }
        private static void readJsonString()
        {
            string json = @"{
'Date':'2015-8-8 15:32:33',
 'CPU':'cpu',
    'PSU': '500W',
   'Drives': [
      'DVD read/writer'
      /*(broken)*/,
      '500 gigabyte hard drive',
      '200 gigabype hard drive'
    ],
'Object':{'Name':'ObjectName'}
}";
            string s = string.Format(@"{{""results"":[{0}]}}", json);
            JObject o = JsonConvert.DeserializeObject<JObject>(s,
                new JsonSerializerSettings()
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    DefaultValueHandling = DefaultValueHandling.Include
                });
            List<NoName> list = new List<NoName>();
            o["results"].ToList().ForEach(p => list.Add(JsonConvert.DeserializeObject<NoName>(p.ToString())));
            // JObject j = JObject.FromObject(o);
            //// j["a"] = "a";
            // j.Add("oo", "dd");
            // j.Add("time", DateTime.Now);
            //List<JObject> list = new List<JObject>();
            //o.ForEach(p => list.Add(JObject.FromObject(p)));
            //list.Add(new NoName() { PSU = "p" ,CPU="c" });
            JArray array = JArray.FromObject(list);
            Console.WriteLine(JsonConvert.SerializeObject(array, Formatting.Indented, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Include }));

        }
        private static JObject ToJObject(object o)
        {
            JObject j = new JObject();
            Type type = o.GetType();
            PropertyInfo[] ps = type.GetProperties();
            //Array.ForEach(ps, p => j[p.Name] = p.GetValue(o, null)) as JToken;
            return j;
        }
        private static void writeMsg()
        {
            new Thread(() =>
               {
                   try
                   {
                       var factory = new ConnectionFactory();
                       factory.HostName = "localhost";
                       factory.UserName = "mq";
                       factory.Password = "mq";

                       using (var connection = factory.CreateConnection())
                       {
                           using (var channel = connection.CreateModel())
                           {
                               var properties = channel.CreateBasicProperties();
                               properties.DeliveryMode = 2;
                               properties.SetPersistent(true);

                               //定义持久化队列
                               channel.QueueDeclare("hello", true, false, false, null);
                               int i = 10;
                               while (i-- > 0)
                               {
                                   string message = "Hello World:" + i;
                                   var body = Encoding.UTF8.GetBytes(message);
                                   channel.BasicPublish("", "hello2", properties, body);

                                   Console.WriteLine("==> {0}", message);
                                   Thread.Sleep(10);
                               }
                           }
                       }
                   }
                   catch (Exception ex)
                   {

                       Console.WriteLine(ex);
                   }
               }).Start();
        }

        private static void readMsg()
        {
            new Thread(() =>
            {
                try
                {


                    var factory = new ConnectionFactory();
                    factory.HostName = "localhost";
                    factory.UserName = "mq";
                    factory.Password = "mq";
                    factory.AutomaticRecoveryEnabled = true;
                    using (var connection = factory.CreateConnection())
                    {
                        using (var channel = connection.CreateModel())
                        {
                            //定义持久化队列
                            channel.QueueDeclare("hello", true, false, false, null);
                            channel.BasicQos(0, 1, false);
                            var consumer = new QueueingBasicConsumer(channel);
                            channel.BasicConsume("hello", false, consumer);

                            Console.WriteLine("waiting for message.");
                            while (true)
                            {
                                var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                                var body = ea.Body;
                                var message = Encoding.UTF8.GetString(body);
                                Console.WriteLine("<== {0}", message);
                                Thread.Sleep(4000);
                                channel.BasicAck(ea.DeliveryTag, false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex);
                }
            }).Start();
        }

        private static void WebRequest()
        {
            try
            {
                //MyTester test = new MyTester();
                //test.Url = "http://localhost/Front/Seckilling/Test_CheckState";
                //test.PostData = "welfareId=24";
                //HttpWebRequest request = test.CreateRequest();
                //TextReader reader = new StreamReader(request.GetResponse().GetResponseStream());

                //Console.WriteLine(reader.ReadToEnd());



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        private static void PrintRows(DataTableReader reader)
        {
            try
            {
                string connStr = "Data Source=(local);Initial Catalog=mydb;integrated security=sspi;Min Pool Size=10";
                DbAccessHelper db = new DbAccessHelper(connStr);

                EventWaitHandle clearCount = new EventWaitHandle(false, EventResetMode.ManualReset);
                int count = 0;
                int maxCount = 2;

                for (int i = 0; i < maxCount; i++)
                {

                    string sql = string.Format("insert into [AOH_SeckillingWinner] (userid,Category) "
                            + " select  ROW_NUMBER() over (order by newid()) ,'{0}' from (select  1 as r   from sys.objects as a "
                            + " cross join   sys.objects  as b) as c", i);
                    Thread t = new Thread((p) =>
                    {
                        // SqlConnection conn = db.GetOpenConnection();
                        Interlocked.Increment(ref count);
                        clearCount.WaitOne();
                        //db.ExcuteNoQuery(conn, p.ToString());
                        Console.WriteLine("finish.");
                        //conn.Close();
                        Interlocked.
                            Decrement(ref count);
                    });
                    t.Start(sql);
                }
                while (count < maxCount)
                {
                    Thread.Sleep(500);
                }

                clearCount.Set();






                //Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }



    }
    public class SeckillingWinnerContact
    {
        [Display(Name = "用户编号")]
        public int UserId { get; set; }
        [Display(Name = "地址")]
        public string UserAddress { get; set; }
        [Display(Name = "手机号")]
        public string UserMobile { get; set; }
        [Display(Name = "姓名")]
        public string UserName { get; set; }
        [Display(Name = "账号")]
        public string UserAlias { get; set; }
        public virtual Person Person { get; set; }
    }
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Sex { get; set; }
        public override string ToString()
        {
            return string.Format("Name:{0},Age:{1},Sex:{2}", Name, Age, Sex);
        }
    }
}
