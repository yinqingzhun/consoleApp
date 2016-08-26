using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyClassLibrary
{
   public class RedisDemo
    {
        public static void Run()
        {
            RedisClient client = new RedisClient("",0);
           
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
