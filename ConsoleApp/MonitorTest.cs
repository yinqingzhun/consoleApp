using System;
using System.Threading;
using System.Collections;

namespace MonitorCS1
{
    class MonitorSample
    {
        const int MAX_LOOP_TIME = 10;
        Queue m_smplQueue;

        public MonitorSample()
        {
            m_smplQueue = new Queue();
        }
        public void FirstThread()
        {
            Thread.Sleep(3000);
            int counter = 0;
            lock (m_smplQueue)
            {
                Console.WriteLine("生产者Run");
                while (counter < MAX_LOOP_TIME)
                {
                    Console.WriteLine("生产者Wait");
                    
                    //Wait, if the queue is busy.
                    Monitor.Wait(m_smplQueue);
                    //Push one element.
                    m_smplQueue.Enqueue(counter);
                    //Release the waiting thread.
                    Monitor.Pulse(m_smplQueue);

                    counter++;
                }
            }
            Console.WriteLine("生产者退出");
        }
        public void SecondThread()
        {
            lock (m_smplQueue)
            {
                Console.WriteLine("消费者Run");
                //Release the waiting thread.
                Monitor.Pulse(m_smplQueue);
                Console.WriteLine("消费者Pulse");
                //Wait in the loop, while the queue is busy.
                //Exit on the time-out when the first thread stops. 
                while (Monitor.Wait(m_smplQueue, 1000))
                {
                    //Pop the first element.
                    int counter = (int)m_smplQueue.Dequeue();
                    //Print the first element.
                    Console.WriteLine(counter.ToString());
                    //Release the waiting thread.
                    Monitor.Pulse(m_smplQueue);
                }
            }
            Console.WriteLine("消费者退出");
        }
        //Return the number of queue elements.
        public int GetQueueCount()
        {
            return m_smplQueue.Count;
        }

          void Main(string[] args)
        {
            //Create the MonitorSample object.
            MonitorSample test = new MonitorSample();
            //Create the first thread.
            Thread tFirst = new Thread(new ThreadStart(test.FirstThread));
            //Create the second thread.
            Thread tSecond = new Thread(new ThreadStart(test.SecondThread));
            //Start threads.
            tFirst.IsBackground = false;
            tSecond.IsBackground = false;
            tFirst.Start();
            tSecond.Start();
            //wait to the end of the two threads
            //tFirst.Join();
            //tSecond.Join();
            //Print the number of queue elements.
            Console.WriteLine("Queue Count = " + test.GetQueueCount().ToString());
            Console.Read();
        }
    }
}