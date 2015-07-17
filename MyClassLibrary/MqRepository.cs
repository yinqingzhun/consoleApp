using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
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

        private Thread _sendMsg = null;
        private readonly List<Thread> _rcvMsgThreadListInExclusiveMode = new List<Thread>();
        private readonly List<Thread> _rcvMsgThreadListInSharingMode = new List<Thread>();
        private readonly Dictionary<MqQueueName, Action<string>> _msgReceiverDic = new Dictionary<MqQueueName, Action<string>>();

        private readonly int MqConcurrentSizeInSharingMode = 5;
        private readonly int MqConcurrentSizeInExclusiveMode = 5;
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

        public void EnableSendMessage()
        {
            if (_sendMsg == null || _sendMsg.ThreadState == ThreadState.Stopped)
            {
                _sendMsg = new Thread(SendMessage);
                _sendMsg.IsBackground = true;
                _sendMsg.Start();
            }
        }

        public void DisableSendMessage()
        {
            if (_sendMsg != null && _sendMsg.ThreadState != ThreadState.Stopped)
            {
                _sendMsg.Abort();
                _sendMsg = null;
            }
        }

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
        private void DisableReceiveMsgInSharingMode()
        {
            foreach (var thread in _rcvMsgThreadListInSharingMode)
            {
                thread.Abort();
            }
            _rcvMsgThreadListInSharingMode.Clear();
        }
        private void DisableReceiveMsgInExclusiveMode()
        {
            foreach (var thread in _rcvMsgThreadListInExclusiveMode)
            {
                thread.Abort();
            }
            _rcvMsgThreadListInExclusiveMode.Clear();
        }


        private void NewConnection()
        {
            var factory = new ConnectionFactory();
            factory.AutomaticRecoveryEnabled = true;
            factory.TopologyRecoveryEnabled = true;
            factory.HostName = "192.168.200.29";
            factory.UserName = "admin";
            factory.Password = "1qazxsw2";

            _connection = factory.CreateConnection();
        }
        /// <summary>
        /// 发送消息到MQ服务器
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="json">JSON格式的消息</param>
        public void SendMessage(MqQueueName queueName, string json)
        {
            if (_queue.Count > _maxLength)
            {
                lock (_queue.SyncRoot)
                {
                    Monitor.Wait(_queue.SyncRoot);
                }
            }

            _queue.Enqueue(queueName + "," + json);
            lock (_queueReadLock)
            {
                Monitor.Pulse(_queueReadLock);
            }
        }

        private void SendMessage()
        {
            IModel channel = null;
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    NewConnection();

                //创建通道
                channel = _connection.CreateModel();

                //设置消息持久化
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                properties.SetPersistent(true);

                while (true)
                {
                    object o = null;
                    try
                    {
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
                            continue;

                        string[] array = o.ToString().Split(new char[] { ',' }, 2);
                        if (array.Length != 2)
                            continue;

                        string queueName = array[0];
                        string json = array[1];
                        //创建队列
                        channel.QueueDeclare(queueName.ToString(), true, false, false, null);
                        //发布消息
                        byte[] body = Encoding.UTF8.GetBytes(json);
                        channel.BasicPublish("", queueName.ToString(), properties, body);
                        lock (_queue.SyncRoot)
                        {
                            Monitor.Pulse(_queue.SyncRoot);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Fatal("RabbitMQ生产者发送消息失败。消息内容：" + (o == null ? "" : o.ToString()), ex);
                    }
                }


            }
            catch (OperationInterruptedException ex)
            {
                LogHelper.Fatal("RabbitMQ生产者发送消息操作中断。", ex);
            }
            catch (Exception ex)
            {
                LogHelper.Fatal("RabbitMQ生产者发送消息失败。", ex);
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
        /// 为消息队列创建多个消费者线程
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

        private void ReceiveMessage(MqQueueName queueName, Action<string> action)
        {
            IModel channel = null;
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    NewConnection();

                //创建通道
                channel = _connection.CreateModel();

                //创建队列
                channel.QueueDeclare(queueName.ToString(), true, false, false, null);

                channel.BasicQos(0, 1, false);
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(queueName.ToString(), false, consumer);

                while (true)
                {
                    string message = "";
                    try
                    {
                        var ea = consumer.Queue.Dequeue();
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
            }
            catch (Exception ex)
            {
                LogHelper.Fatal("RabbitMQ接收和解析消息失败。", ex);
            }
            finally
            {
                if (channel != null)
                {
                    channel.Close();
                }
            }
        }

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

        private void ReceiveMessage()
        {

            Array array = Enum.GetValues(typeof(MqQueueName));


            IModel channel = null;
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    NewConnection();

                //创建通道
                channel = _connection.CreateModel();

                while (true)
                {
                    foreach (var item in array)
                    {

                        MqQueueName queueName = (MqQueueName)item;

                        Action<string> action = null;
                        if (!_msgReceiverDic.ContainsKey(queueName))
                            continue;

                        action = _msgReceiverDic[queueName];

                        //创建队列
                        channel.QueueDeclare(queueName.ToString(), true, false, false, null);

                        channel.BasicQos(0, 1, false);


                        string message = "";
                        try
                        {
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
            }
            catch (Exception ex)
            {
                LogHelper.Fatal("RabbitMQ接收和解析消息失败。", ex);
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
    public class LogHelper
    {
        static LogHelper()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
        public static void Fatal(string msg, Exception ex)
        {
            log4net.LogManager.GetLogger("").Fatal(msg, ex);
        }
    }
    public enum MqQueueName
    {
        ClubTopic,
        ClubPost,
        ForumTopic,
        Attention,
    }
}
