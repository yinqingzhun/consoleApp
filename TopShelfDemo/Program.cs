using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Config;
using Topshelf;

namespace TopShelfDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            log4net.LogManager.GetLogger("program").Error("error!!");
            HostFactory.Run(x =>
            {
                x.Service<SendMsgService>(s =>
                {
                    s.ConstructUsing(name => new SendMsgService());
                    s.WhenStarted((t) => t.Start());
                    s.WhenStopped((t) => t.Stop());
                });

                x.RunAsLocalSystem();
                
                //服务的描述
                x.SetDescription("TopshelfDemo");
                //服务的显示名称
                x.SetDisplayName("TopshelfDemo");
                //服务名称
                x.SetServiceName("TopshelfDemo");
                 
            });

        }
    }
}
