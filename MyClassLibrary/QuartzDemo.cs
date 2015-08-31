using Common.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClassLibrary
{
   public class QuartzDemo
    {
       public static void Run()
       {
           ILog log = LogManager.GetLogger(typeof(QuartzDemo));

           log.Info("------- Initializing ----------------------");

           // First we must get a reference to a scheduler
           ISchedulerFactory sf = new StdSchedulerFactory();
           IScheduler sched = sf.GetScheduler();

           log.Info("------- Initialization Complete -----------");

           log.Info("------- Scheduling Jobs -------------------");

           // computer a time that is on the next round minute
           //DateTime runTime = TriggerUtils.GetEvenMinuteDate(new NullableDateTime(DateTime.Now));

           //// define the job and tie it to our HelloJob class
           //JobDetail job = new JobDetail("job1", "group1", typeof(HelloJob));

           //// Trigger the job to run on the next round minute
           //SimpleTrigger trigger = new SimpleTrigger("trigger1", "group1", runTime);

           //// Tell quartz to schedule the job using our trigger
           //sched.ScheduleJob(job, trigger);
           //log.Info(string.Format("{0} will run at: {1}", job.FullName, runTime.ToString("r")));

           //// Start up the scheduler (nothing can actually run until the 
           //// scheduler has been started)
           //sched.Start();
           //log.Info("------- Started Scheduler -----------------");

           //// wait long enough so that the scheduler as an opportunity to 
           //// run the job!
           //log.Info("------- Waiting 90 seconds -------------");

           //// wait 90 seconds to show jobs
           //Thread.Sleep(90 * 1000);

           // shut down the scheduler
           log.Info("------- Shutting Down ---------------------");
           sched.Shutdown(true);
           log.Info("------- Shutdown Complete -----------------");
       }
    }
}
