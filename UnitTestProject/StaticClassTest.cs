using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fakes;
using InterfaceProjectForTest.Fakes;
using InterfaceProjectForTest;

namespace UnitTestProject
{
    [TestClass]
    public class StaticClassTest
    {
        [TestMethod]
        public void MyMethodTest()
        {
            using (ShimsContext.Create())
            {
                ShimMyClass.Random = () => 5;
                Assert.IsTrue(MyClass.Random() == 5);

                var o = new ShimMyClass() { GetName = () => { return "o1"; } };
                ShimMyClass.AllInstances.GetName = (p) => "o";

                Assert.AreEqual(((MyClass)o).GetName(), "o1");
                Assert.AreEqual(new MyClass().GetName(), "o");

                //var shim = new ShimMyClass();
                ShimMyClass.ConstructorInt32 = (@this, value) =>
                {
                    var shim = new ShimMyClass(@this)
                    {
                        ValueGet = () => -5
                    };
                };

                Assert.AreEqual(new MyClass(6).Value, -5);
            }
        }
    }

   
}
