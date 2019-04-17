using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IPA.LoggerManager;
using System.Reflection;

namespace IPA.MainApp.UnitTest
{
	[TestClass]
    public class LoggerManager : UnitTestBase
    {
        [TestClass]
        public class PositiveTests
        { 
		    [TestMethod]
		    public void InitalizeLogger()
		    {
                string fullName = "LoggerTest";
                string logname = System.IO.Path.GetFileNameWithoutExtension(fullName) + ".log";
                string path = System.IO.Directory.GetCurrentDirectory(); 
                string filepath = path + "\\" + logname;
			    int logLevel = (int)LOGLEVELS.DEBUG;
			    Logger.SetFileLoggerConfiguration(filepath, logLevel);
			    Assert.IsTrue(logLevel == Logger.GetFileLoggerLevel());
		    }
        }

        [TestClass]
        public class NegativeTests
        {
            [TestMethod]
		    public void InitalizeLogger()
		    {
                string fullName = "LoggerTest";
                string logname = System.IO.Path.GetFileNameWithoutExtension(fullName) + ".log";
                string path = System.IO.Directory.GetCurrentDirectory(); 
                string filepath = path + "\\" + logname;
			    int logLevel = (int)LOGLEVELS.DEBUG;
			    Logger.SetFileLoggerConfiguration(filepath, logLevel);
			    Assert.IsTrue((int)LOGLEVELS.INFO == Logger.GetFileLoggerLevel());
		    }
        }
    }
}
