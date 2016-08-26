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
using System.Web;
using System.Security.Cryptography;


namespace ConsoleApp
{
    //程序入口类
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
                IISHelper.SetWebSitePath("auto", @"G:\deploy\auto");
                //CopyHelper.Copy("h:\\pic", "h:\\ii");
                //readMsg();
                //writeMsg();
                //HandleMqMsg();
                //PartitionerDemo.Run();
                //readJsonString();




                Console.WriteLine("done");

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
            Console.WriteLine(JsonConvert.SerializeObject(array, Formatting.Indented));

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
                       factory.UserName = "guest";
                       factory.Password = "guest";

                       using (var connection = factory.CreateConnection())
                       {
                           using (var channel = connection.CreateModel())
                           {
                               var properties = channel.CreateBasicProperties();
                               properties.DeliveryMode = 2;
                               properties.Persistent = true;

                               //定义持久化队列
                               channel.QueueDeclare("hello", true, false, false, null);
                               int i = 10;
                               while (true)
                               {
                                   while (i-- > 0)
                                   {
                                       string message = "Hello World:" + i;
                                       var body = Encoding.UTF8.GetBytes(message);
                                       channel.BasicPublish("", "hello", properties, body);

                                       Console.WriteLine("==> {0}", message);
                                       Thread.Sleep(10);
                                   }
                                   Console.WriteLine("press any key to push 10 more messages...");
                                   Console.ReadKey();
                                   i = 10;
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
    public class TopicInfo
    {
        List<TopicVideoInfo> _videos = new List<TopicVideoInfo>();
        List<TopicPhotoInfo> _photos = new List<TopicPhotoInfo>();
        public int Tid { get; set; }

        public int Cid { get; set; }

        public int PosterId { get; set; }
        private string _posterName = "";
        public string PosterName
        {
            get { return _posterName ?? ""; }
            set { _posterName = value; }
        }

        public DateTime PostDateTime { get; set; }

        public int UpCount { get; set; }

        public int ReplyCount { get; set; }

        public int SourceType { get; set; }
        private string _postIp = "";
        public string PostIp
        {
            get { return _postIp ?? ""; }
            set { _postIp = value; }
        }
        private string _content = "";
        public string Content
        {
            get { return _content ?? ""; }
            set { _content = value; }
        }

        public int PhotoCount { get; set; }

        public bool IsPhoto { get; set; }

        public int Mileage { get; set; }
        private string _topicGuid = "";
        public string TopicGuid
        {
            get { return _topicGuid ?? ""; }
            set { _topicGuid = value; }
        }

        public int FilterStatus { get; set; }

        public int Status { get; set; }

        public List<TopicPhotoInfo> Photos
        {
            get
            {
                return _photos;
            }
            set
            {
                _photos = value;
            }
        }
        private string _extentInfo = "";
        public string ExtentInfo
        {
            get { return _extentInfo ?? ""; }
            set { _extentInfo = value; }
        }

        public bool IsDigest { get; set; }

        public bool IsTop { get; set; }

        public int PosterRole { get; set; }
        private string _avatar60 = "";
        public string Avatar60
        {
            get { return _avatar60 ?? ""; }
            set { _avatar60 = value; }
        }

        public bool IsVideo { get; set; }

        public int VideoCount { get; set; }

        public List<TopicVideoInfo> Videos
        {
            get
            {
                return _videos;
            }
            set
            {
                _videos = value;
            }
        }

        public int ClientSourceType { get; set; }

        public int CommentLimit { get; set; }

        public int ParentTid { get; set; }
        private string _clubName;
        public string ClubName
        {
            get { return _clubName ?? ""; }
            set { _clubName = value; }
        }

        public int ForwardCount { get; set; }
        public TopicInfo ForwardTopic { get; set; }
        private string _city;
        public string City
        {
            get { return _city ?? ""; }
            set { _city = value; }
        }

        public bool IsIdentification { get; set; }
    }

    public class TopicPhotoInfo
    {
        public int Id { get; set; }

        public int Tid { get; set; }

        public int Cid { get; set; }
        private string _photoPath;
        public string PhotoPath
        {
            get { return _photoPath ?? ""; }
            set { _photoPath = value; }
        }

        public int UserId { get; set; }

        public DateTime PostDateTime { get; set; }
        private string _photoType;
        public string PhotoType
        {
            get { return _photoType ?? ""; }
            set { _photoType = value; }
        }

        public int PhotoSize { get; set; }
        private string _sourceName;
        public string SourceName
        {
            get { return _sourceName ?? ""; }
            set { _sourceName = value; }
        }

        public int PhotoWidth { get; set; }

        public int PhotoHeight { get; set; }
        private string _photoExif;
        public string PhotoExif
        {
            get { return _photoExif ?? ""; }
            set { _photoExif = value; }
        }
        private string _photoGuid;
        public string PhotoGuid
        {
            get { return _photoGuid ?? ""; }
            set { _photoGuid = value; }
        }
        public int Status { get; set; }
        private string _showUrl;
        public string ShowUrl
        {
            get { return _showUrl ?? ""; }
            set { _showUrl = value; }
        }
    }

    public class TopicVideoInfo
    {
        public int Id { get; set; }
        public int Tid { get; set; }
        public string VideoUrl { get; set; }
        public string VideoCover { get; set; }
        public string VideoTitle { get; set; }
        public int Status { get; set; }
    }
}
