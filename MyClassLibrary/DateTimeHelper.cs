using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyClassLibrary
{
    public class DateTimeHelper
    {
        /// <summary>
        /// 距离当前时间多久
        /// </summary>
        /// <param name="time1"></param>
        /// <returns></returns>
        public static string GetTimeEXTSpan(DateTime? time1)
        {
            if (time1 == null) return "";
            string strTime = "";
            DateTime date1 = DateTime.Now;
            DateTime date2 = (DateTime)time1;
            TimeSpan dt = date1 - date2;

            // 相差天数
            int days = dt.Days;
            // 时间点相差小时数
            int hours = dt.Hours;
            // 相差总小时数
            double Minutes = dt.Minutes;
            // 相差总秒数
            int second = dt.Seconds;

            if (days == 0 && hours == 0 && Minutes == 0)
            {
                strTime = "刚刚";
            }
            else if (days == 0 && hours == 0)
            {
                strTime = Minutes + "分钟前";
            }
            else if (days == 0)
            {
                strTime = hours + "小时前";
            }
            else
            {
                strTime = ((DateTime)time1).ToString("MM月dd日");
            }
            return strTime;
        }

        /// <summary>
        /// 格式化时间
        /// </summary>
        /// <param name="data"></param>
        /// <param name="formattype"></param>
        /// <returns></returns>
        public static string Format(DateTime? date, string formattype)
        {
            if (date == null) return "";
            try
            {
                return ((DateTime)date).ToString(formattype);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 对比两时间之差
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <param name="type">d天,h小时,m分钟,s秒</param>
        /// <returns></returns>
        public static string Diff(DateTime? date1, DateTime? date2, string type)
        {
            if (date1 == null) return "";
            if (date2 == null) return "";
            DateTime _date1 = (DateTime)date1;
            DateTime _date2 = (DateTime)date2;
            TimeSpan dt = _date1 - _date2;

            // 相差天数
            int days = dt.Days;
            // 时间点相差小时数
            int hours = dt.Hours;
            // 相差总小时数
            double Minutes = dt.Minutes;
            // 相差总秒数
            int second = dt.Seconds;

            string rStr = "";
            switch (type)
            {
                case "d":
                    rStr = dt.Days.ToString();
                    break;
                case "h":
                    rStr = dt.Hours.ToString();
                    break;
                case "m":
                    rStr = dt.Minutes.ToString();
                    break;
                case "s":
                    rStr = dt.Seconds.ToString();
                    break;
            }
            return rStr;
        }

        /// <summary>
        /// 获取当前月天数
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int MonthDays(DateTime? date)
        {
            if (date == null) return 0;
            DateTime dtNow = (DateTime)date;

            int days = dtNow.AddDays(1 - dtNow.Day).AddMonths(1).AddDays(-1).Day;
            return days;
        }

        /// <summary>
        /// 判断当前月是否为闰月
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool IsLeapYear(DateTime? date)
        {
            if (date == null) return false;
            DateTime dtNow = (DateTime)date;
            return (0 == dtNow.Year % 4 && ((dtNow.Year % 100 != 0) || (dtNow.Year % 400 == 0)));
        }

        /// <summary>
        /// 当天当前时间是否到达指定的时间
        /// </summary>
        /// <remarks>仅比较DateTime的时间部分，不比较DateTime的日期部分</remarks>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool IsTimeUpToday(int hour, int minute, int second)
        {
            DateTime now = DateTime.Now;
            var sometimeToday = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);
            return (now - sometimeToday).Ticks > 0;
        }
        /// <summary>
        /// 返回当天当前时间距离指定的时间的时间差
        /// </summary>
        /// <remarks>仅比较DateTime的时间部分，不比较DateTime的日期部分</remarks>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns>时间差，单位：毫秒</returns>
        public static TimeSpan DiffToday(int hour, int minute, int second)
        {
            DateTime now = DateTime.Now;
            var sometimeToday = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);
            return sometimeToday - now;
        }
        /// <summary>
        /// 显示简短日期格式
        /// </summary>
        /// <remarks>今天显示时间部分，昨天显示昨天和时间部分，其余显示完整日期</remarks>
        /// <returns></returns>
        public static string ShowShortDateTime(DateTime datetime)
        {
            DateTime today = DateTime.Now.Date;
            if (today == datetime.Date)
                return datetime.ToString("HH:mm");
            else if (today.AddDays(-1) == datetime.Date)
                return datetime.ToString("昨天 HH:mm");
            else if (today.Year == datetime.Year)
                return datetime.ToString("MM-dd");
            else
                return datetime.ToString("yy-MM-dd");
        }

        public static long ToUnixTimestamp(DateTime datetime)
        {
            long seconds = (datetime.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            return seconds;
        }

        public static long ToUnixTimestampOfNow()
        {
            return ToUnixTimestamp(DateTime.Now);
        }

        public static DateTime ToUniversalTime(long unixTimestamp)
        {
            return new DateTime(unixTimestamp * 10000000 + 621355968000000000);
        }
    }
}
