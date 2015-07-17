﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class TaskDemo
    {
        public static void Run()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<int> t = new Task<int>(() => Add(cts.Token), CancellationToken.None);
            t.Start();
            t.ContinueWith(TaskEnded);
            //等待按下任意一个键取消任务
            Console.ReadKey();
            cts.Cancel();
            Console.ReadKey();
        }

        static void TaskEnded(Task<int> task)
        {
            Console.WriteLine("任务完成，完成时候的状态为：");
            Console.WriteLine("IsCanceled={0}\tIsCompleted={1}\tIsFaulted={2}", task.IsCanceled, task.IsCompleted, task.IsFaulted);
            Console.WriteLine("任务的返回值为：{0}", task.Result);
        }

        static int Add(CancellationToken ct)
        {
            Console.WriteLine("任务开始……");
            int result = 0;
            while (!ct.IsCancellationRequested)
            {
                result++;
                Thread.Sleep(1000);
            }
            Console.WriteLine("准备结束...");
            return result;
        }

        public static void Run2()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<int> t = new Task<int>(() => AddCancleByThrow(cts.Token), cts.Token);
            t.Start();
            t.ContinueWith(TaskEndedByCatch);
            //等待按下任意一个键取消任务
            Console.ReadKey();
            cts.Cancel();
            Console.ReadKey();
        }

        static void TaskEndedByCatch(Task<int> task)
        {
            Console.WriteLine("任务完成，完成时候的状态为：");
            Console.WriteLine("IsCanceled={0}\tIsCompleted={1}\tIsFaulted={2}", task.IsCanceled, task.IsCompleted, task.IsFaulted);
            try
            {
                Console.WriteLine("任务的返回值为：{0}", task.Result);
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.Message);
                e.Handle((err) => err is OperationCanceledException);
            }
        }

        static int AddCancleByThrow(CancellationToken ct)
        {
            Console.WriteLine("任务开始……");
            int result = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                result++;
                Thread.Sleep(1000);
            }
            return result;
        }

        public static void Run3()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            //等待按下任意一个键取消任务
            TaskFactory taskFactory = new TaskFactory();
            Task[] tasks = new Task[]
                {
                    taskFactory.StartNew(() => Add(cts.Token)),
                    taskFactory.StartNew(() => Add(cts.Token)),
                    taskFactory.StartNew(() => Add(cts.Token))
                };
            //CancellationToken.None指示TasksEnded不能被取消
            taskFactory.ContinueWhenAll(tasks, TasksEnded, CancellationToken.None);
            Console.ReadKey();
            cts.Cancel();
            Console.ReadKey();
        }

        static void TasksEnded(Task[] tasks)
        {
            Console.WriteLine("所有任务已完成！");
        }
    }
}
