using ServiceStack.Redis;
using ServiceStack.Text;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MyClassLibrary
{
    public class Todo
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public bool Done { get; set; }
    }
    public class RedisDemo
    {
        private static void t()
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisTodos = redis.As<Todo>();
                var todo = new Todo
                {
                    Id = redisTodos.GetNextSequence(),
                    Content = "Learn Redis",
                    Order = 1,
                };

                redisTodos.Store(todo);

                Todo savedTodo = redisTodos.GetById(todo.Id);
                savedTodo.Done = true;
                redisTodos.Store(savedTodo);

                "Updated Todo:".Print();
                redisTodos.GetAll().ToList().PrintDump();

                redisTodos.DeleteById(savedTodo.Id);

                "No more Todos:".Print();
                redisTodos.GetAll().ToList().PrintDump();
            }
        }
        public static void Run()
        {
            ConnectionMultiplexer conn = ConnectionMultiplexer.Connect("localhost:6379");
            {
                var client = conn.GetDatabase();

                client.KeyDelete("re");
                client.KeyDelete("rd");
                var d = client.SortedSetScore("re", "user0");
                Console.WriteLine(d == null ? "NaN" : d.ToString());


                //producer
                for (int i = 0; i < 3; i++)
                {
                    Thread producer = new Thread((o) =>
                    {
                        Random r = new Random(i);
                        while (true)
                        {
                            string name = "user" + o;

                            if (client.SortedSetIncrement("re", name, 1) == 1)
                                client.SortedSetAdd("rd", name, DateTimeHelper.ToUnixTimestampOfNow());

                            d = client.SortedSetScore("re", name);
                            Console.WriteLine(name + ":" + (d == null ? "NaN" : d.ToString()));

                            Thread.Sleep(r.Next(3000));
                        }
                    });
                    producer.IsBackground = true;
                    producer.Start(i);
                }


                ////consumer
                Thread countConsumer = new Thread(() =>
                {
                    while (true)
                    {

                        var list = client.SortedSetRangeByRankWithScores("re", 0, 0, Order.Descending);

                        if (list.Count() > 0)
                        {
                            var item = list.FirstOrDefault();
                            if (item.Score >= 3)
                            {
                                Console.WriteLine("send mail by count.\t" + string.Format("name:{0}, count={1}", item.Element, item.Score));

                                var trans = client.CreateTransaction();
                                trans.SortedSetRemoveAsync("re", item.Element);
                                trans.SortedSetRemoveAsync("rd", item.Element);
                                trans.Execute();

                                continue;
                            }
                        }
                        Thread.Sleep(1000);


                    }
                });
                countConsumer.Start();


                Thread timeConsumer = new Thread(() =>
                {
                    while (true)
                    {

                        var list = client.SortedSetRangeByRankWithScores("rd", 0, 0, Order.Descending);

                        if (list.Count() > 0)
                        {
                            var item = list.FirstOrDefault();
                            var ts = DateTime.UtcNow - DateTimeHelper.ToUniversalTime((long)item.Score);
                            if (ts.TotalSeconds > 2)
                            {
                                Console.WriteLine("send mail by duration.\t" + string.Format("name:{0}, duration={1:f1}s", item.Element, ts.TotalSeconds));
                                var trans = client.CreateTransaction();
                                trans.SortedSetRemoveAsync("re", item.Element);
                                trans.SortedSetRemoveAsync("rd", item.Element);
                                trans.Execute();

                                continue;
                            }
                        }
                        Thread.Sleep(1000);


                    }
                });
                timeConsumer.Start();
            }




        }

        public static void Run1()
        {
            using (var redisManager = new PooledRedisClientManager(new string[] { "localhost:6379" }, new string[] { "localhost:6379" }, new RedisClientManagerConfig()
            {
                AutoStart = true,
                MaxReadPoolSize = 10,
                MaxWritePoolSize = 10
            }))
            {

                //client.Set("name", Encoding.UTF8.GetBytes("Jack"));
                //Console.WriteLine(client.Get<string>("name"));
                using (var client = redisManager.GetClient())
                {
                    client.Remove("re");
                    client.Remove("rd");
                    var d = client.GetItemScoreInSortedSet("re", "user0");
                    Console.WriteLine(double.IsNaN(d) ? "NaN" : d.ToString());
                }

                //producer
                for (int i = 0; i < 3; i++)
                {
                    Thread producer = new Thread((o) =>
                    {
                        Random r = new Random(i);
                        while (true)
                        {
                            string name = "user" + o;
                            using (var client = redisManager.GetClient())
                            {
                                if (client.IncrementItemInSortedSet("re", name, 1) == 1)
                                    client.AddItemToSortedSet("rd", name, DateTimeHelper.ToUnixTimestampOfNow());

                                var d = client.GetItemScoreInSortedSet("re", name);
                                Console.WriteLine(name + ":" + (double.IsNaN(d) ? "NaN" : d.ToString()));
                            }
                            Thread.Sleep(r.Next(3000));
                        }
                    });
                    producer.IsBackground = true;
                    producer.Start(i);
                }


                ////consumer
                Thread countConsumer = new Thread(() =>
                {
                    while (true)
                    {
                        using (var client = redisManager.GetClient())
                        {
                            var list = client.GetRangeWithScoresFromSortedSetDesc("re", 0, 0);

                            if (list.Count > 0)
                            {
                                var item = list.FirstOrDefault();
                                if (item.Value >= 3)
                                {
                                    Console.WriteLine("send mail by count.\t" + string.Format("name:{0}, count={1}", item.Key, item.Value));

                                    using (var trans = client.CreateTransaction())
                                    {
                                        trans.QueueCommand(r => r.RemoveItemFromSortedSet("re", item.Key));
                                        trans.QueueCommand(r => r.RemoveItemFromSortedSet("rd", item.Key));

                                        trans.Commit();
                                    }

                                    continue;
                                }
                            }
                            Thread.Sleep(1000);
                        }

                    }
                });
                countConsumer.Start();


                Thread timeConsumer = new Thread(() =>
                {
                    while (true)
                    {
                        using (var client = redisManager.GetClient())
                        {
                            var list = client.GetRangeWithScoresFromSortedSet("rd", 0, 0);

                            if (list.Count > 0)
                            {
                                var item = list.FirstOrDefault();
                                var ts = DateTime.UtcNow - DateTimeHelper.ToUniversalTime((long)item.Value);
                                if (ts.TotalSeconds > 2)
                                {
                                    Console.WriteLine("send mail by duration.\t" + string.Format("name:{0}, duration={1:f1}s", item.Key, ts.TotalSeconds));
                                    using (var trans = client.CreateTransaction())
                                    {
                                        trans.QueueCommand(r => r.RemoveItemFromSortedSet("re", item.Key));
                                        trans.QueueCommand(r => r.RemoveItemFromSortedSet("rd", item.Key));

                                        trans.Commit();
                                    }
                                    continue;
                                }
                            }
                            Thread.Sleep(1000);
                        }

                    }
                });
                timeConsumer.Start();
            }




        }


    }
    ///// <summary>
    ///// Redis实体基类，所有redis实体类都应该集成它
    ///// </summary>
    //public abstract class RedisEntity
    //{
    //    public RedisEntity()
    //    {
    //        RootID = Guid.NewGuid().ToString();
    //    }
    //    /// <summary>
    //    /// Redis实体主键，方法查询，删除，更新等操作
    //    /// </summary>
    //    public virtual string RootID { get; set; }
    //}
    ///// <summary>
    ///// Redis仓储实现
    ///// </summary>
    //public class RedisRepository<TEntity> :
    //    IDisposable,
    //    IRepository<TEntity>
    //    where TEntity : RedisEntity
    //{
    //    IRedisClient redisDB;
    //    IRedisTypedClient<TEntity> redisTypedClient;
    //    IRedisList<TEntity> table;
    //    public RedisRepository()
    //    {
    //        redisDB = RedisManager.GetClient();
    //        redisTypedClient = redisDB.GetTypedClient<TEntity>();
    //        table = redisTypedClient.Lists[typeof(TEntity).Name];
    //    }

    //    #region IRepository<TEntity>成员
    //    public void SetDbContext(IUnitOfWork unitOfWork)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Insert(TEntity item)
    //    {
    //        if (item != null)
    //        {
    //            redisTypedClient.AddItemToList(table, item);
    //            redisDB.Save();
    //        }

    //    }

    //    public void Delete(TEntity item)
    //    {
    //        if (item != null)
    //        {
    //            var entity = Find(item.RootID);
    //            redisTypedClient.RemoveItemFromList(table, entity);
    //            redisDB.Save();
    //        }
    //    }

    //    public void Update(TEntity item)
    //    {
    //        if (item != null)
    //        {
    //            var old = Find(item.RootID);
    //            if (old != null)
    //            {
    //                redisTypedClient.RemoveItemFromList(table, old);
    //                redisTypedClient.AddItemToList(table, item);
    //                redisDB.Save();
    //            }
    //        }
    //    }

    //    public IQueryable<TEntity> GetModel()
    //    {
    //        return table.GetAll().AsQueryable();
    //    }

    //    public TEntity Find(params object[] id)
    //    {
    //        return table.Where(i => i.RootID == (string)id[0]).FirstOrDefault();
    //    }
    //    #endregion

    //    #region IDisposable成员
    //    public void Dispose()
    //    {
    //        this.ExplicitDispose();
    //    }
    //    #endregion

    //    #region Protected Methods

    //    /// <summary>
    //    /// Provides the facility that disposes the object in an explicit manner,
    //    /// preventing the Finalizer from being called after the object has been
    //    /// disposed explicitly.
    //    /// </summary>
    //    protected void ExplicitDispose()
    //    {
    //        this.Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected void Dispose(bool disposing)
    //    {
    //        if (disposing)//清除非托管资源
    //        {
    //            table = null;
    //            redisTypedClient = null;
    //            redisDB.Dispose();
    //        }
    //    }
    //    #endregion

    //    #region Finalization Constructs
    //    /// <summary>
    //    /// Finalizes the object.
    //    /// </summary>
    //    ~RedisRepository()
    //    {
    //        this.Dispose(false);
    //    }
    //    #endregion
    //}

}
