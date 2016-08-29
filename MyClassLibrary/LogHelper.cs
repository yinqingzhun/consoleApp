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
            log4net.Config.XmlConfigurator.Configure();
        }
        public static void Fatal(string msg, Exception ex = null, string logger = null)
        {
            log4net.LogManager.GetLogger(logger).Fatal(msg + (ex == null ? "" : "异常信息：" + ex.ToString()));
        }

        public static void Error(string msg, Exception ex = null, string logger = null)
        {
            log4net.LogManager.GetLogger(logger).Error(msg + (ex == null ? "" : "异常信息：" + ex.ToString()));
        }

        public static void Info(string msg, Exception ex = null, string logger = null)
        {
            log4net.LogManager.GetLogger(logger).Info(msg + (ex == null ? "" : "异常信息：" + ex.ToString()));
        }

        public static void Debug(string msg, Exception ex = null, string logger = null)
        {
            log4net.LogManager.GetLogger(logger).Debug(msg + (ex == null ? "" : "异常信息：" + ex.ToString()));
        }

        public static void Warn(string msg, Exception ex = null, string logger = null)
        {
            log4net.LogManager.GetLogger(logger).Warn(msg + (ex == null ? "" : "异常信息：" + ex.ToString()));
        }
    }
}
