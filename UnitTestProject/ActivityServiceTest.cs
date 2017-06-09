using System;
using System.Fakes;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InterfaceProjectForTest;

namespace UnitTestProject
{
    [TestClass]
    public class ActivityServiceTest
    {
        [TestMethod]
        public void IsExpireTest()
        {
            ActivityService service = new ActivityService();
            bool actual = service.IsExpire();
            Assert.IsTrue(actual);

            using (ShimsContext.Create())
            {
                ShimDateTime.NowGet = () => new DateTime(2004, 5, 5);
                actual = service.IsExpire();
                Assert.IsFalse(actual);
            }
        }
    }
}
