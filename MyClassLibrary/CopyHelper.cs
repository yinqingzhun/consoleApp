using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
namespace MyClassLibrary
{
    public class CopyHelper
    {
        public static void Copy(string solutionDirectory, string targetDirectory, string exclude = "")
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            //Give the name as Xcopy
            startInfo.FileName = "xcopy.exe";
            //make the window Hidden
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            string cmd = string.Format(" \"{0}\" \"{1}\" /i/e/v/Y", solutionDirectory, targetDirectory);
            if (File.Exists(exclude))
                cmd = string.Format("{0} /EXCLUDE:'{1}'", cmd, exclude);
            //Send the Source and destination as Arguments to the process
            startInfo.Arguments = cmd;
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                   Console.WriteLine(exeProcess.StandardOutput.ReadToEnd());
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }

        }
    }
}
