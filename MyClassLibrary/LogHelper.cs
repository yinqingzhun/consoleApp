using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MyClassLibrary
{
    public class LogHelper
    {
        static LogHelper()
        {
            var configFileInfo = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config"));
            if (configFileInfo.Exists)
            {
                XmlConfigurator.ConfigureAndWatch(configFileInfo);
            }
            else
            {
                XmlConfigurator.Configure();
            }
        }


        public static void Debug(string message, string loggerName = "")
        {
            Debug(message, null, loggerName);
        }

        public static void Debug(string message, Exception ex, string loggerName = "")
        {
            try
            {
                var logger = LogManager.GetLogger(loggerName);
                logger.Debug(message, ex);
            }
            catch
            {
                // ignored
            }
        }

        public static void Info(string message, string loggerName = "")
        {
            Info(message, null, loggerName);
        }

        public static void Info(string message, Exception ex, string loggerName = "")
        {
            try
            {
                var logger = LogManager.GetLogger(loggerName);
                logger.Info(message, ex);
            }
            catch
            {
                // ignored
            }
        }

        public static void Warn(string message, string loggerName = "")
        {
            Warn(message, null, loggerName);
        }

        public static void Warn(string message, Exception ex, string loggerName = "")
        {
            try
            {
                var logger = LogManager.GetLogger(loggerName);
                logger.Warn(message, ex);
            }
            catch
            {
                // ignored
            }
        }
        public static void Error(string message, string loggerName = "")
        {
            Error(message, null, loggerName);
        }
        public static void Error(string message, Exception ex , string loggerName = "")
        {
            try
            {
                var logger = LogManager.GetLogger(loggerName);
                if (ex == null)
                {
                    logger.Error(message, ex);
                }
                else
                {
                    logger.Error(message, ex);
                }
            }
            catch
            {
                // ignored
            }
        }
        public static void Fatal(string message, string loggerName = "")
        {
            Fatal(message, null, loggerName);
        }
        public static void Fatal(string message, Exception ex , string loggerName = "")
        {
            try
            {
                var logger = LogManager.GetLogger(loggerName);
                if (ex == null)
                {
                    logger.Fatal(message, ex);
                }
                else
                {
                    logger.Fatal(message, ex);
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
