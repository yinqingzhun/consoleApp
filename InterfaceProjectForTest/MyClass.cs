using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfaceProjectForTest
{
    public class MyClass
    {
        public static MyClass Instance
        {
            get
            {
                return new MyClass();
            }

        }
        public MyClass()
        {

        }

        public MyClass(int v)
        {
            Value = v;
        }
        public static int Random()
        {
            return new Random().Next();
        }

        public int Value { get; set; }

        public string GetName()
        {
            return string.Empty;
        }


    }
}
