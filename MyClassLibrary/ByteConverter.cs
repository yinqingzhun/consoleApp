using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyClassLibrary
{
    public class ByteArrayConverter
    {
        /// <summary>
        /// 将字节数组转为十六进制字符串
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static string BytesToString(byte[] bs)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bs.Length; i++)
                sb.AppendFormat("{0:X2}", bs[i]);
            return sb.ToString();
        }

        public static byte[] StringToBytes(string s)
        {
            byte[] bs = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i = i + 2)
            {
                bs[i / 2] = Convert.ToByte(s.Substring(i, 2), 0x10);
            }
            return bs;
        }
    }
}
