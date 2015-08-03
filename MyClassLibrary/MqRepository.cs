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

        private Thread _sendMsg = null;
        private readonly List<Thread> _rcvMsgThreadListInExclusiveMode = new List<Thread>();
        private readonly List<Thread> _rcvMsgThreadListInSharingMode = new List<Thread>();
        private readonly Dictionary<MqQueueName, Action<string>> _msgReceiverDic = new Dictionary<MqQueueName, Action<string>>();

        private readonly int MqConcurrentSizeInSharingMode = 5;
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

            Thread t = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    int run = 0;
                    int idle = 0;
                    _rcvMsgThreadListInExclusiveMode.ForEach(p =>
                    {
                        if (p.ThreadState == ThreadState.Running)
                            run++;
                        else
                            idle++;
                    });
                    Console.WriteLine("~~~running:" + run + ", idle:" + idle);
                }
            });

            t.Start();
        }
        /// <summary>
        /// 启动消息发送支持
        /// </summary>
        public void EnableSendMessage()
        {
            if (_sendMsg == null || _sendMsg.ThreadState == ThreadState.Stopped)
            {
                _sendMsg = new Thread(SendMessage);
                //_sendMsg.IsBackground = true;
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
        /// 创建与消息队列的连接
        /// </summary>
        private void NewConnection()
        {
            if (_connection == null)
            {
                lock (_queue)
                {
                    if (_connection == null)
                    {
                        try
                        {
                            var factory = new ConnectionFactory();
                            factory.AutomaticRecoveryEnabled = true;
                            factory.TopologyRecoveryEnabled = true;
                            factory.HostName = "localhost";
                            factory.UserName = "mq";
                            factory.Password = "mq";

                            _connection = factory.CreateConnection();

                        }
                        catch (Exception ex)
                        {
                            LogHelper.Fatal("创建与MQ服务器的Connection失败。", ex);
                        }

                    }
                }
            }
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
        /// <summary>
        /// 从内存队列中读取消息，并发送至MQ服务器
        /// </summary>
        private void SendMessage()
        {
            IModel channel = null;
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    NewConnection();

                if (_connection == null)
                {
                    LogHelper.Fatal("IConnection为空，消息接收线程将退出。");
                    return;
                }

                //创建通道
                channel = _connection.CreateModel();

                //设置消息持久化
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                properties.SetPersistent(true);

                while (true)
                {
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
                        continue;

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

                    lock (_queue.SyncRoot)
                    {
                        Monitor.Pulse(_queue.SyncRoot);
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
                //thread.IsBackground = true;
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
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    NewConnection();

                if (_connection == null)
                {
                    LogHelper.Fatal("IConnection为空，消息接收线程将退出。");
                    return;
                }

                //创建通道
                channel = _connection.CreateModel();

                //创建队列
                channel.QueueDeclare(queueName.ToString(), true, false, false, null);

                channel.BasicQos(0, 1, false);
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(queueName.ToString(), false, consumer);
                BasicDeliverEventArgs ea = null;
                while (true)
                {
                    string message = "";
                    try
                    {
                        if (channel.IsClosed)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        //连接断开之后，重新连接时，需要重新打开队列
                        if (consumer.ShutdownReason != null && channel.IsOpen)
                        {
                            channel.Close();
                            channel = _connection.CreateModel();
                            channel.QueueDeclare(queueName.ToString(), true, false, false, null);
                            channel.BasicQos(0, 1, false);
                            consumer = new QueueingBasicConsumer(channel);
                            channel.BasicConsume(queueName.ToString(), false, consumer);
                        }

                        consumer.Queue.Dequeue(100, out ea);
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
                //thread.IsBackground = true;
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
                if (_connection == null || !_connection.IsOpen)
                    NewConnection();

                if (_connection == null)
                {
                    LogHelper.Fatal("IConnection为空，消息接收线程将退出。");
                    return;
                }

                //创建通道
                channel = _connection.CreateModel();

                while (true)
                {
                    foreach (var item in array)
                    {
                        if (channel.IsClosed)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

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
    public class LogHelper
    {
        static LogHelper()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
        public static void Fatal(string msg, Exception ex = null)
        {
            log4net.LogManager.GetLogger("").Fatal(msg + (ex == null ? "" : "异常信息：" + ex.Message));
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
