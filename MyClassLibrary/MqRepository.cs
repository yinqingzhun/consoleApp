using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MyClassLibrary;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace ConsoleApp
{
    public class MqRepository
    {
        private static MqRepository _instance = new MqRepository();
        private IConnection _connection = null;
        private Queue _queue = Queue.Synchronized(new Queue());
        private int _maxLength = 10000;
        private object _queueReadLock = new object();
        private object _newConnectionLock = new object();
        private TimeSpan _reconnectionInterval = TimeSpan.FromSeconds(5);

        private Thread _sendMsg = null;
        private readonly List<Thread> _rcvMsgThreadListInExclusiveMode = new List<Thread>();
        private readonly List<Thread> _rcvMsgThreadListInSharingMode = new List<Thread>();
        private readonly Dictionary<MqQueueName, Action<string>> _msgReceiverDic = new Dictionary<MqQueueName, Action<string>>();

        private readonly int MqConcurrentSizeInSharingMode = 10;
        private readonly int MqConcurrentSizeInExclusiveMode = 2;


        public static MqRepository Instance
        {
            get
            {
                return _instance;
            }
        }

        private MqRepository()
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains("MqConcurrentSizeInExclusiveMode"))
                int.TryParse(ConfigurationManager.AppSettings["MqConcurrentSizeInExclusiveMode"], out MqConcurrentSizeInExclusiveMode);

            if (ConfigurationManager.AppSettings.AllKeys.Contains("MqConcurrentSizeInSharingMode"))
                int.TryParse(ConfigurationManager.AppSettings["MqConcurrentSizeInSharingMode"], out MqConcurrentSizeInSharingMode);

            NewConnection();
            EnableSendMessage();
        }
        /// <summary>
        /// 启动消息发送支持
        /// </summary>
        public void EnableSendMessage()
        {
            if (_sendMsg == null || _sendMsg.ThreadState == ThreadState.Stopped)
            {
                _sendMsg = new Thread(SendMessage);
                _sendMsg.IsBackground = true;
                _sendMsg.Start();
            }
        }
        /// <summary>
        /// 停止消息发送支持
        /// </summary>
        public void DisableSendMessage()
        {
            if (_sendMsg != null && _sendMsg.ThreadState != ThreadState.Stopped)
            {
                _sendMsg.Abort();
                _sendMsg = null;
            }
        }
        /// <summary>
        /// 停止从MQ取消息
        /// </summary>
        public void DisableReceiveMsg()
        {
            foreach (var thread in _rcvMsgThreadListInExclusiveMode)
            {
                thread.Abort();
            }
            _rcvMsgThreadListInExclusiveMode.Clear();

            foreach (var thread in _rcvMsgThreadListInSharingMode)
            {
                thread.Abort();
            }
            _rcvMsgThreadListInSharingMode.Clear();
        }
        /// <summary>
        /// 停止共享模式下的MQ消息获取
        /// </summary>
        private void DisableReceiveMsgInSharingMode()
        {
            foreach (var thread in _rcvMsgThreadListInSharingMode)
            {
                thread.Abort();
            }
            _rcvMsgThreadListInSharingMode.Clear();
        }
        /// <summary>
        /// 停止独享模式下的MQ消息获取
        /// </summary>
        private void DisableReceiveMsgInExclusiveMode()
        {
            foreach (var thread in _rcvMsgThreadListInExclusiveMode)
            {
                thread.Abort();
            }
            _rcvMsgThreadListInExclusiveMode.Clear();
        }

        /// <summary>
        /// 创建与消息队列服务器的连接
        /// </summary>
        /// <returns>是否创建了新连接</returns>
        private bool NewConnection()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                lock (_newConnectionLock)
                {
                    if (_connection == null || !_connection.IsOpen)
                    {
                        MqServers.ConnectionInfo info = MqServers.Instance.Next();
                        if (info == null)
                            throw new ArgumentNullException("connectionInfo", "连接MQ服务器的信息不存在");
                        try
                        {
                            var factory = new ConnectionFactory();
                            factory.AutomaticRecoveryEnabled = false;
                            factory.TopologyRecoveryEnabled = false;
                            factory.HostName = info.HostName;
                            factory.Port = info.Port;
                            factory.UserName = info.UserName;
                            factory.Password = info.Password;

                            _connection = factory.CreateConnection();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Fatal("创建与MQ服务器的Connection失败。" + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                        }

                    }
                }
            }

            return false;
        }
        /// <summary>
        /// 发送消息到MQ服务器
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="json">JSON格式的消息</param>
        public void SendMessage(MqQueueName queueName, string json)
        {
            //消息缓存满时，丢弃新消息
            if (_queue.Count > _maxLength)
                return;

            _queue.Enqueue(queueName + "," + json);
            lock (_queueReadLock)
            {
                Monitor.Pulse(_queueReadLock);
            }
        }
        /// <summary>
        /// 从内存队列中读取消息，并发送至MQ服务器
        /// </summary>
        private void SendMessage()
        {
            IModel channel = null;
            IBasicProperties properties = null;
            try
            {
                while (true)
                {
                    try
                    {
                        if (_connection == null || !_connection.IsOpen)
                            NewConnection();

                        //连接失败时，5秒后重试
                        if (_connection == null)
                        {
                            Thread.Sleep(_reconnectionInterval);
                            continue;
                        }

                        if (channel == null || channel.IsClosed)
                        {
                            //创建通道
                            try
                            {
                                channel = _connection.CreateModel();
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Fatal("RabbitMQ消息生产者创建通道失败：" + ex.Message);
                                continue;
                            }


                            //设置消息持久化
                            properties = channel.CreateBasicProperties();
                            properties.DeliveryMode = 2;
                            properties.Persistent=true;
                        }


                        object o = null;

                        if (_queue.Count == 0)
                        {
                            lock (_queueReadLock)
                            {
                                Monitor.Wait(_queueReadLock);
                            }
                        }
                        //从内存队列中取消息
                        o = _queue.Dequeue();
                        if (o == null)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        string[] array = o.ToString().Split(new char[] { ',' }, 2);
                        if (array.Length != 2)
                        {
                            LogHelper.Fatal("RabbitMQ生产者收到未识别的消息：" + o);
                            continue;
                        }

                        string queueName = array[0];
                        string json = array[1];
                        try
                        {
                            //创建队列
                            channel.QueueDeclare(queueName.ToString(), true, false, false, null);
                            //发布消息
                            byte[] body = Encoding.UTF8.GetBytes(json);
                            channel.BasicPublish("", queueName.ToString(), properties, body);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Fatal("RabbitMQ生产者发送消息失败。消息内容：" + (o == null ? "" : o.ToString()), ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Fatal("RabbitMQ生产者发送消息失败。" + ex.Message);
                    }
                }


            }
            catch (OperationInterruptedException ex)
            {
                LogHelper.Fatal("RabbitMQ生产者发送消息操作中断。", ex);
            }
            catch (Exception ex)
            {
                LogHelper.Fatal("RabbitMQ生产者发送消息发生异常，线程退出。", ex);
            }
            finally
            {
                if (channel != null)
                {
                    channel.Close();
                }
            }
        }
        #region 接收消息
        /// <summary>
        /// 使用独享模式接收消息。独享模式为每一个消息队列都创建多个消息读取线程
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="action"></param>
        public void ReceiveMessageByExclusiveMode(MqQueueName queueName, Action<string> action)
        {
            if (_msgReceiverDic.ContainsKey(queueName))
                return;
            else
                _msgReceiverDic.Add(queueName, action);

            DisableReceiveMsgInSharingMode();
            BatchReceiveMessage(queueName, action, MqConcurrentSizeInExclusiveMode);
        }
        /// <summary>
        /// 使用共享模式接收消息。共享模式创建多个消息读取线程，用于所有消息队列的消息读取
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="action"></param>
        public void ReceiveMessageBySharingMode(MqQueueName queueName, Action<string> action)
        {
            if (_msgReceiverDic.ContainsKey(queueName))
                return;
            else
                _msgReceiverDic.Add(queueName, action);

            DisableReceiveMsgInExclusiveMode();
            if (_rcvMsgThreadListInSharingMode.Count == 0)
                BatchReceiveMessage(MqConcurrentSizeInSharingMode);
        }
        /// <summary>
        /// 为指定的消息队列创建多个消费者
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="action"></param>
        /// <param name="batchSize"></param>
        private void BatchReceiveMessage(MqQueueName queueName, Action<string> action, int batchSize)
        {
            for (int i = 0; i < batchSize; i++)
            {
                Thread thread = new Thread((o) =>
                {
                    object[] os = o as object[];
                    ReceiveMessage((MqQueueName)os[0], os[1] as Action<string>);
                });
                thread.IsBackground = true;
                thread.Start(new object[] { queueName, action });
                _rcvMsgThreadListInExclusiveMode.Add(thread);
            }
        }
        /// <summary>
        /// 从消息队列中读取消息，并处理
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="action"></param>
        private void ReceiveMessage(MqQueueName queueName, Action<string> action)
        {
            IModel channel = null;
            QueueingBasicConsumer consumer = null;
            BasicDeliverEventArgs ea = null;
            string message = "";

            try
            {
                while (true)
                {
                    try
                    {
                        if (_connection == null || !_connection.IsOpen)
                            NewConnection();

                        //连接失败时，5秒后重试
                        if (_connection == null)
                        {
                            Thread.Sleep(_reconnectionInterval);
                            continue;
                        }

                        if (channel == null || channel.IsClosed)
                        {
                            try
                            {
                                channel = _connection.CreateModel();
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Fatal("RabbitMQ消息生产者创建通道失败：" + ex.Message);
                                continue;
                            }

                            //创建队列
                            channel.QueueDeclare(queueName.ToString(), true, false, false, null);
                            channel.BasicQos(0, 1, false);

                            //创建消费者
                            consumer = new QueueingBasicConsumer(channel);
                            channel.BasicConsume(queueName.ToString(), false, consumer);
                        }


                        try
                        {
                            //获取消息
                            ea = consumer.Queue.Dequeue();
                            message = Encoding.UTF8.GetString(ea.Body);
                            //处理消息
                            action(message);
                            //发送确认回执
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Fatal(string.Format("RabbitMQ消费者处理消息失败。消息：queueName={0},message={1}。", queueName, message), ex);
                        }

                    }
                    catch (Exception ex)
                    {
                        LogHelper.Fatal("RabbitMQ消费者处理消息失败。" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Fatal("RabbitMQ接收和解析消息发生异常，线程退出。", ex);
            }
            finally
            {
                if (channel != null)
                {
                    channel.Close();
                }
            }
        }
        /// <summary>
        /// 创建多个消费者，用于所有队列的数据读取
        /// </summary>
        /// <param name="batchSize"></param>
        private void BatchReceiveMessage(int batchSize)
        {
            for (int i = 0; i < batchSize; i++)
            {
                Thread thread = new Thread(ReceiveMessage);
                thread.IsBackground = true;
                thread.Start();
                _rcvMsgThreadListInSharingMode.Add(thread);
            }
        }
        /// <summary>
        /// 遍历指定队列，并从队列中读取数据
        /// </summary>
        private void ReceiveMessage()
        {

            Array array = Enum.GetValues(typeof(MqQueueName));

            IModel channel = null;
            try
            {
                while (true)
                {
                    try
                    {
                        if (_connection == null || !_connection.IsOpen)
                            NewConnection();

                        //连接失败时，5秒后重试
                        if (_connection == null)
                        {
                            Thread.Sleep(_reconnectionInterval);
                            continue;
                        }

                        if (channel == null || channel.IsClosed)
                        {
                            try
                            {
                                channel = _connection.CreateModel();
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Fatal("RabbitMQ消息生产者创建通道失败：" + ex.Message);
                                continue;
                            }
                        }


                        foreach (var item in array)
                        {
                            MqQueueName queueName = (MqQueueName)item;

                            Action<string> action = null;
                            if (!_msgReceiverDic.ContainsKey(queueName))
                                continue;

                            action = _msgReceiverDic[queueName];

                            string message = "";
                            try
                            {
                                //创建队列
                                channel.QueueDeclare(queueName.ToString(), true, false, false, null);
                                channel.BasicQos(0, 1, false);

                                var ea = channel.BasicGet(queueName.ToString(), false);
                                if (ea == null)
                                    continue;

                                message = Encoding.UTF8.GetString(ea.Body);
                                //处理消息
                                action(message);
                                //发送确认回执
                                channel.BasicAck(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Fatal("RabbitMQ消费者处理消息失败。消息内容：" + queueName + "," + message, ex);
                            }
                        }

                        Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Fatal("RabbitMQ消费者处理消息失败。" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Fatal("RabbitMQ接收和解析消息发生异常，线程退出。", ex);
            }
            finally
            {
                if (channel != null)
                {
                    channel.Close();
                }
            }
        }
        #endregion
    }
  
    public enum MqQueueName
    {
        ClubTopic,
        ClubPost,
        ForumTopic,
        Attention,
    }

    public class MqServers
    {
        private const int DefautPort = -1;
        private List<ConnectionInfo> _list = new List<ConnectionInfo>();
        private int _currentIndex = 0;
        private static MqServers _instance = new MqServers();
        public static MqServers Instance
        {
            get
            {
                return _instance;
            }
        }
        private MqServers()
        {
            string hosts = ConfigurationManager.AppSettings["Service.RabitMq.Host"];
            string userName = ConfigurationManager.AppSettings["Service.RabitMq.User"];
            string password = ConfigurationManager.AppSettings["Service.RabitMq.Password"];
            string[] hostArray = hosts.Split(new string[] { ",", "|", ";" }, StringSplitOptions.RemoveEmptyEntries);
            Array.ForEach(hostArray, p => AddServer(p, userName, password));
        }
        private bool ExistServer(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                return false;
            return _list.Exists(p => p.HostName == hostName);
        }
        private void AddServer(string hostName, int port, string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                return;

            if (port == 0 || port < -1 || port > 65535)
                return;

            if (!ExistServer(hostName))
                _list.Add(new ConnectionInfo() { HostName = hostName, Port = port, UserName = userName, Password = password });

        }

        private void AddServer(string hostName, string userName, string password)
        {
            AddServer(hostName, DefautPort, userName, password);
        }

        private void RemoveServer(string hostName, int port = DefautPort)
        {
            ConnectionInfo conn = _list.SingleOrDefault(p => p.HostName == hostName && p.Port == DefautPort);
            if (conn != null)
                _list.Remove(conn);
        }

        public ConnectionInfo Next()
        {
            int count = _list.Count;
            if (count == 0)
                return null;

            return _list[_currentIndex++ % count];
        }

        public class ConnectionInfo
        {
            public string HostName { get; set; }
            public int Port { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }



    }
}
