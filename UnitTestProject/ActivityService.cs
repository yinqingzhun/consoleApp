using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTestProject
{
    public class ActivityService
    {
        public DateTime BeginTime { get; set; }

        public ActivityService()
        {
            this.BeginTime = new DateTime(2014, 3, 3);  //仅作演示，无意义
        }

        public bool IsExpire()
        {
            return BeginTime >= DateTime.Now;
        }
    }
}
