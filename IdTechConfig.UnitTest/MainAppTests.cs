using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace IPA.MainApp.UnitTest
{
	[TestClass]
    public class MainApp : UnitTestBase
    {
        [TestClass]
        public class PositiveTests
        { 
		    [TestMethod]
		    public void InitializeDevice()
		    {
                //Assert.IsNotNull();
		    }
        }

        [TestClass]
        public class NegativeTests
        {
            [TestMethod]
		    public void InitializeDevice()
		    {
			    //Assert.IsNotNull();
		    }
        }
    }
}
