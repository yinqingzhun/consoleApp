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
        public static void Main()
        {

            try
            {
                //readMsg();
                //writeMsg();
                HandleMqMsg();

                //Console.WriteLine(Convert.FromBase64String);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }
        private static void Json()
        {

            string s = "{'a':1,'b':[{'a':1},{'b':2}]}";
            JObject j = JsonConvert.DeserializeObject<JObject>(s);
            JToken n = j["b"].Children().AsEnumerable().ToList().First();
            Console.WriteLine(n["a"]);
        }
        private static void HandleMqMsg()
        {
            Action<string> a = p =>
            {
                Console.WriteLine(p);
                Thread.Sleep(500);
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
        private static IConnection NewConnection()
        {
            var factory = new ConnectionFactory();
            factory.HostName = "localhost";
            factory.UserName = "mq";
            factory.Password = "mq";
            factory.AutomaticRecoveryEnabled = true;
            factory.TopologyRecoveryEnabled = true;
            return factory.CreateConnection();
        }
        private static void writeMsg()
        {
            new Thread(() =>
               {
                   try
                   {
                       using (var connection = NewConnection())
                       {
                           using (var channel = connection.CreateModel())
                           {
                               var properties = channel.CreateBasicProperties();
                               properties.DeliveryMode = 2;
                               properties.SetPersistent(true);

                               //定义持久化队列
                               channel.QueueDeclare("hello", true, false, false, null);
                               channel.ConfirmSelect();
                               int i = 10;
                               while (true)
                               {
                                   string message = "Hello World:" + i;
                                   var body = Encoding.UTF8.GetBytes(message);
                                   try
                                   {
                                       channel.BasicPublish("", "hello", properties, body);
                                       channel.WaitForConfirmsOrDie();
                                   }
                                   catch (Exception ex)
                                   {
                                       Console.WriteLine(ex.Message);
                                   }

                                   Console.WriteLine("==> {0}", message);
                                   Thread.Sleep(10);
                                   i--;
                                   if (i <= 0)
                                   {
                                       Console.WriteLine("press any key to continue...");
                                       Console.ReadKey();
                                       i = 10;
                                   }
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
                    using (var connection = NewConnection())
                    {
                        var channel = connection.CreateModel();

                        //定义持久化队列
                        channel.QueueDeclare("hello", true, false, false, null);
                        channel.BasicQos(0, 1, false);
                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume("hello", false, consumer);

                        Console.WriteLine("waiting for message.");
                        BasicDeliverEventArgs ea = null;
                        while (true)
                        {
                            try
                            {
                                if (consumer.ShutdownReason != null && channel.IsOpen)
                                {
                                    channel.Close();
                                    channel = connection.CreateModel();
                                    channel.QueueDeclare("hello", true, false, false, null);
                                    channel.BasicQos(0, 1, false);
                                    consumer = new QueueingBasicConsumer(channel);
                                    channel.BasicConsume("hello", false, consumer);
                                }

                                ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();


                                var body = ea.Body;
                                var message = Encoding.UTF8.GetString(body);
                                Console.WriteLine("<== {0}", message);
                                //Thread.Sleep(4000);

                                channel.BasicAck(ea.DeliveryTag, false);
                                ea = null;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }

                            Console.WriteLine("press any key to continue...");
                            Console.ReadKey();
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
