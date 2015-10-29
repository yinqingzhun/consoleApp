using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace MyClassLibrary
{
    public class DosCommandHelper
    {
        public static string Execute(string fileName, string arguments)
        {
            string output = string.Empty;
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            //Give the name as Xcopy
            startInfo.FileName = fileName;
            //make the window Hidden
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            //Send the Source and destination as Arguments to the process
            startInfo.Arguments = arguments;
            
            try
            {

                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                //当前登录用户不是管理员时
                if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    startInfo.Verb = "runas";
                }
                
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    output = exeProcess.StandardOutput.ReadToEnd();
                    Console.WriteLine(output);
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }
            return output;
        }
    }

    public class IISHelper
    {
        public static void SetWebSitePath(string websiteName, string websitePath)
        {
            const string cmdName = @"C:\Windows\System32\inetsrv\appcmd.exe";
            string arguments = string.Format("set app \"{0}/\" -[path='/'].physicalPath:\"{1}\"", websiteName, websitePath);
            DosCommandHelper.Execute(cmdName, arguments);
        }
    }
}
