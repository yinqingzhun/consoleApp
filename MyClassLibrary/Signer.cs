using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClassLibrary
{
    public class Signer
    {
        private readonly string _paramsValueSecret = "abcb040d991e4e8598e8b9baed2c070d";
        private const string ParamsNameTimestamp = "timestamp";
        private static Dictionary<int, string> dic = new Dictionary<int, string>();
        static Signer()
        {
            dic.Add(902, "请求签名错误");
            dic.Add(903, "请求未提供时间戳参数");
            dic.Add(905, "你的时间和服务器时间差别太大，是穿越了吗？");
        }
        public static string GetValidErrorInfo(int errorCode)
        {
            if (dic.ContainsKey(errorCode))
                return dic[errorCode];
            return string.Empty;
        }
        public static void Run(string queryString)
        {
            var signer = new Signer();
            Dictionary<string, object> dic = new Dictionary<string, object>();
            Array.ForEach(queryString.Split('&'), p =>
            {
                string[] s = p.Split('=');
                dic.Add(s[0], s[1]);
            });

            var signName = "sign";
            if (!dic.ContainsKey(signName))
            {
                Console.WriteLine("缺少sign");
                return;
            }
            var sign = Convert.ToString(dic[signName] ?? "");
            dic.Remove(signName);

            var newDic = new Dictionary<string, object>();
            dic.Keys.ToList().ForEach(p => newDic.Add(p, System.Web.HttpUtility.UrlEncode(dic[p].ToString())));

            int errorCode = 0;
            if (!new Signer().Valid(dic, sign, out errorCode))
            {
                Console.WriteLine(errorCode + ":" + Signer.GetValidErrorInfo(errorCode));
                return;
            }
            Console.WriteLine("OK");

        }
        public bool Valid(IDictionary<string, object> data, string sign, out int errorCode)
        {
            errorCode = 0;
            if (!data.ContainsKey(ParamsNameTimestamp))
            {
                errorCode = 903;
                return false;
            }

            var timer = Convert.ToInt64(data[ParamsNameTimestamp]);

            var epoch = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;

            var diff = Math.Abs(epoch - timer);

            //if (diff / 60 > 600)
            //{
            //    errorCode = 905;
            //    return false;
            //}

            var ret = Generate(data);
            bool b = Generate(data) == sign.ToUpper();
            if (!b)
                errorCode = 902;
            return b;
        }
        public string Generate(IDictionary<string, object> data)
        {
            var sort = new SortedDictionary<string, object>(data);
            var ret = new StringBuilder();

            foreach (var item in sort)
            {
                ret.Append(item.Key);
                ret.Append(item.Value);
            }

            ret.Insert(0, _paramsValueSecret);
            ret.Append(_paramsValueSecret);

            var sign = MD5(ret.ToString()).ToUpper();

            return sign;
        }
        public static string MD5(string context)
        {
            byte[] ret = Encoding.UTF8.GetBytes(context);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(ret);
            return BitConverter.ToString(output).Replace("-", "");
        }
    }
}
