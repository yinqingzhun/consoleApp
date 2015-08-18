using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.Memcached;

namespace ConsoleApp
{
    //安装：命令行输入 'c:/memcached/memcached.exe -d install'
    //启动：命令行输入 'c:/memcached/memcached.exe -d start' ，默认监听端口为 11211
    // 更改端口和内存大小 memcached.exe -p 11211 -m 64
    public class MemCachedDemo
    {
        private static MemcachedClient client = new MemcachedClient("enyim.com/memcached");
        public static void Add(string key, object value, int expiresInSecond)
        {
            var r = client.ExecuteStore(StoreMode.Add, key, value, TimeSpan.FromSeconds(expiresInSecond));
            var s = r.Success;
        }

        public static T Get<T>(string key)
        {
            return client.Get<T>(key);
        }

        public static void Remove(string key)
        {
            client.Remove(key);
        }
        public static void Run()
        {
            var detail = new IdentificationDetail()
            {
                BrandId = 1,
                BrandName = "hello",
            };
            object name = client.Get("name");
            if (name == null)
            {
                bool result = client.Store(StoreMode.Add, "name", detail);
                //带过期时间的缓存    
                //bool success = client.Store(StoreMode.Add, "userName", "", DateTime.Now.AddMinutes(10));
                if (result)
                {
                    Console.WriteLine("成功存入缓存");
                }
                else
                {
                    Console.WriteLine("存入缓存失败");
                }

            }
            //取值    
            name = client.Get("name");
            if (name != null)
            {
                Console.WriteLine("取出的值为:" + name);
            }
            else
            {
                Console.WriteLine("取值失败");
            }

        }
    }
    [SerializableAttribute] 
    public class IdentificationDetail
    {
        /// <summary>
        /// 是否已认证
        /// </summary>
        public bool IsIdentification { get; set; }

        public int BrandId { get; set; }
        public int SerialId { get; set; }
        public int CarId { get; set; }
        public string BrandName { get; set; }
        public string SerialName { get; set; }
        public string CarName { get; set; }
    }
}
