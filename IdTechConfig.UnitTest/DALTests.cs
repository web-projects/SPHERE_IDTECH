﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace IPA.MainApp.UnitTest
{
	[TestClass]
    public class DAL : UnitTestBase
    {
        [TestClass]
        public class PositiveTests
        { 
		    [TestMethod]
		    public void DeviceConnect()
		    {
                //Assert.IsNotNull();
		    }
        }

        [TestClass]
        public class NegativeTests
        {
            [TestMethod]
		    public void DeviceConnect()
		    {
			    //Assert.IsNotNull();
		    }
        }
    }
}
