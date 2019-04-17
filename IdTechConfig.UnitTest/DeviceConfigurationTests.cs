using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace IPA.MainApp.UnitTest
{
	[TestClass]
    public class DeviceConfiguration : UnitTestBase
    {
        [TestClass]
        public class PositiveTests
        { 
		    [TestMethod]
		    public void DeviceInit()
		    {
                //Assert.IsNotNull();
		    }
        }

        [TestClass]
        public class NegativeTests
        {
            [TestMethod]
		    public void DeviceInit()
		    {
			    //Assert.IsNotNull();
		    }
        }
    }
}
