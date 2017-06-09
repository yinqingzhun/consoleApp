using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterfaceProjectForTest
{
    public class ActivityService
    {
        public DateTime DeadlineTime { get; set; }

        public ActivityService()
        {
            this.DeadlineTime = new DateTime(2014, 3, 3);  //仅作演示，无意义
        }

        public bool IsExpire()
        {
            return DeadlineTime <= DateTime.Now;
        }
    }
}
