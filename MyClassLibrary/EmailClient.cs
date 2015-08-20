using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MyClassLibrary
{
    public class EmailClient
    {
        private static string smtpHost = "";
        private static int smtpPort = 25;
        private static string senderEmail = "";
        private static string[] receiverEmail = new string[0];
        private static string userName = "";
        private static string password = "";
        public static void Run()
        {
            senderEmail = ConfigurationHelper.GetAppSetting<string>("ExceptionStatLog_SenderEmailAddress");

            string s = ConfigurationHelper.GetAppSetting<string>("ExceptionStatLog_ReceiverEmailAddress");
            receiverEmail = s.Split(new char[] { ',', ':', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (ConfigurationManager.AppSettings.AllKeys.Contains("ExceptionStatLog_SmtpHost"))
                smtpHost = ConfigurationManager.AppSettings["ExceptionStatLog_SmtpHost"];

            if (ConfigurationManager.AppSettings.AllKeys.Contains("ExceptionStatLog_SmtpPort"))
                int.TryParse(ConfigurationManager.AppSettings["ExceptionStatLog_SmtpPort"], out smtpPort);

            if (ConfigurationManager.AppSettings.AllKeys.Contains("ExceptionStatLog_UserName"))
                userName = ConfigurationManager.AppSettings["ExceptionStatLog_UserName"];

            if (ConfigurationManager.AppSettings.AllKeys.Contains("ExceptionStatLog_Password"))
                password = ConfigurationManager.AppSettings["ExceptionStatLog_Password"];


            Sendmail(senderEmail, receiverEmail, "hello", "Hello! This is a test email.", smtpHost, smtpPort, userName, password);
        }
        private static void Sendmail(string senderAddress, string[] receiverAddress, string subject, string body, string smtpHost, int smtpPort, string userName, string pwd)
        {
            MailMessage oMail = null;
            SmtpClient client = null;
            try
            {
                string rcvr = receiverAddress.Length > 0 ? receiverAddress[0] : "";

                MailAddress from = new MailAddress(senderAddress, "");
                MailAddress to = new MailAddress(rcvr, "");
                oMail = new MailMessage(from, to);

                oMail.Subject = subject;
                oMail.Body = body;
                oMail.BodyEncoding = System.Text.Encoding.UTF8;
                oMail.Priority = MailPriority.High;

                if (receiverAddress.Length > 1)
                {
                    for (int i = 1; i < receiverAddress.Length; i++)
                        oMail.CC.Add(receiverAddress[i]);
                }

                client = new SmtpClient(smtpHost, smtpPort);
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(userName, pwd);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                client.Send(oMail);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (FormatException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("发送异常监控日志失败。" + ex);
            }
            finally
            {
                oMail.Dispose();
                client.Dispose();
            }

        }

    }
}
