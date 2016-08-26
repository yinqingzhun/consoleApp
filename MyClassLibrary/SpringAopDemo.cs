using AopAlliance.Intercept;
using Spring.Aop;
using Spring.Context;
using Spring.Context.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace MyClassLibrary
{
   public class SpringAopDemo
    {
        public static void Run()
        {
            IApplicationContext ctx = ContextRegistry.GetContext();
            var speakerDictionary = ctx.GetObjectsOfType(typeof(IHelloWorldSpeaker)) ;
            foreach (var entry in speakerDictionary)
            {
                string name = (string)entry.Key;
                IHelloWorldSpeaker worldSpeaker = (IHelloWorldSpeaker)entry.Value;
                Console.Write(name + " says; ");
                worldSpeaker.SayHello();
                Console.WriteLine("-----");
            }

            ISpeakerDao dao = (ISpeakerDao)ctx.GetObject("speakerDao");
            IList speakerList = dao.FindAll();
            IHelloWorldSpeaker speaker = dao.Save(new HelloWorldSpeaker());


        }
    }

    public interface ICommand
    {
        object Execute(object context);
    }

    public class ServiceCommand : ICommand
    {
        public object Execute(object context)
        {
            Console.Out.WriteLine("Service implementation : [{0}]", context);
            return null;
        }
    }

    public class ConsoleLoggingAroundAdvice : IMethodInterceptor
    {
        public object Invoke(IMethodInvocation invocation)
        {
            

            Console.Out.WriteLine("Advice executing; calling the advised method..."); 
            object returnValue = invocation.Proceed(); 
            Console.Out.WriteLine("Advice executed; advised method returned " + returnValue); 
            return returnValue; 
        }
    }

    public class AfterMethodAdvice : IAfterReturningAdvice
    {
        public void AfterReturning(object returnValue, MethodInfo method, object[] args, object target)
        {
            throw new NotImplementedException();
        }
    }

    public enum Language
    {
        English = 1,
        Portuguese = 2,
        Italian = 3
    }

    public interface IHelloWorldSpeaker
    {
        void SayHello();
    }

    public class HelloWorldSpeaker : IHelloWorldSpeaker
    {
        private Language language;

        public Language Language
        {
            set { language = value; }
            get { return language; }
        }

        public void SayHello()
        {
            switch (language)
            {
                case Language.English:
                    Console.WriteLine("Hello World!");
                    break;
                case Language.Portuguese:
                    Console.WriteLine("Oi Mundo!");
                    break;
                case Language.Italian:
                    Console.WriteLine("Ciao Mondo!");
                    break;
            }
        }
    }

    public class DebugInterceptor : IMethodInterceptor
    {
        public object Invoke(IMethodInvocation invocation)
        {
            Console.WriteLine("Before: " + invocation.Method.ToString());
            object rval = invocation.Proceed();
            Console.WriteLine("After:  " + invocation.Method.ToString());
            return rval;
        }
    }

    public class TimingInterceptor : IMethodInterceptor
    {
        public object Invoke(IMethodInvocation invocation)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            var retval = invocation.Proceed();
            watch.Stop();
            
            Console.WriteLine("Elapsed time is {0}ms.", watch.Elapsed.TotalMilliseconds);
            return retval;
        }
    }

    public interface ISpeakerDao
    {
        IList FindAll();

        IHelloWorldSpeaker Save(IHelloWorldSpeaker speaker);
    }

    public class SpeakerDao : ISpeakerDao
    {
        public System.Collections.IList FindAll()
        {
            Console.WriteLine("Finding speakers...");
            // just a demo...fake the retrieval.
            Thread.Sleep(2000);
            HelloWorldSpeaker speaker = new HelloWorldSpeaker();
            speaker.Language = Language.Portuguese;

            IList list = new ArrayList();
            list.Add(speaker);
            return list;
        }
        [ConsoleDebug]
        public IHelloWorldSpeaker Save(IHelloWorldSpeaker speaker)
        {
            Console.WriteLine("Saving speaker...");
            // just a demo...not really saving...
            return speaker;
        }
    }

    public class ConsoleDebugAttribute : Attribute
    {

    }


}
