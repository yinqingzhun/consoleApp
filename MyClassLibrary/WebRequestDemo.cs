using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyClassLibrary
{
    public class WebRequestDemo
    {
        public static void RunHttpRequest()
        {
            // Create a request for the URL.         
            WebRequest request = WebRequest.Create("http://www.google.com.hk");
           Console.WriteLine( request.Timeout );
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                // Display the status.

                Console.WriteLine("StatusDescription: " + response.StatusDescription);

                Console.WriteLine("ResponseText:");
                // Get the stream containing content returned by the server.
                using (Stream dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        // Read the content.
                        string responseFromServer = reader.ReadToEnd();
                        // Display the content.
                        Console.WriteLine(responseFromServer);
                        // Cleanup the streams and the response.
                    }
                }
            }
        }
    }

}
