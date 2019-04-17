using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IPA.CommonInterface.ConfigSphere;

namespace IPA.MainApp.UnitTest
{
	[TestClass]
    public class CommonInterface : UnitTestBase
    {
        [TestClass]
        public class PositiveTests
        { 
		    [TestMethod]
		    public void LoadConfiguration()
		    {
                ConfigSphereSerializer SphereSerializer = new ConfigSphereSerializer();
                SphereSerializer.ReadConfig();
                Assert.IsNotNull(SphereSerializer.terminalCfg);
		    }
        }

        [TestClass]
        public class NegativeTests
        {
            [TestMethod]
		    public void LoadConfiguration()
		    {
                ConfigSphereSerializer SphereSerializer = new ConfigSphereSerializer();
			    Assert.IsNotNull(SphereSerializer.terminalCfg);
		    }
        }
    }
}
