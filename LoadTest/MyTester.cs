using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpTest4Net.Interfaces;

namespace LoadTest
{
    [Test("CheckState_Post")]
    public class MyTester : IUrlTester
    {
        public MyTester()
        {
        }

        public string Url { get; set; }



        private static long mIndex = 0;
        private static List<string> mWords;


        /*          {"query" :     {  "bool" : {    "should" : [ {      "field" : {        "title" : "#key"      }    }, {      "field" : {        "kw" : "#key"      }    } ]  }    },from:0,size:10}         */


        private static int _index = 0;
        public System.Net.HttpWebRequest CreateRequest()
        {
            int ni = Interlocked.Increment(ref _index);
            string newUrl = Url;
            string[] hostArray = Hosts.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (!Url.StartsWith("http://") && hostArray.Length > 0)
            {
                newUrl = "http://" + hostArray[ni % hostArray.Length] + (Url.StartsWith("/") ? "" : "/") + Url;
            }
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(newUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            string json = (string.IsNullOrWhiteSpace(PostData) ? "" : PostData + "&") + "userId=" +ni ;
            httpWebRequest.ContentLength = Encoding.UTF8.GetByteCount(json);
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
            }
            return httpWebRequest;
        }

        public TestType Type
        {
            get { return TestType.POST; }
        }

        public string PostData { get; set; }
        public string Hosts { get; set; }
    }
}