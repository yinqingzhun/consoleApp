using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClassLibrary
{
    public class ConfigurationHelper
    {

        public static T GetAppSetting<T>(string key, T defaultValue = default(T))
        {
            try
            {
                return (T)Convert.ChangeType(GetAppSetting(key), typeof(T));
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex);
            }

            return defaultValue;

        }
        public static string GetAppSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            if (ConfigurationManager.AppSettings.AllKeys.Contains(key))
                return ConfigurationManager.AppSettings[key];
            throw new KeyNotFoundException(key);
        }
    }
}
