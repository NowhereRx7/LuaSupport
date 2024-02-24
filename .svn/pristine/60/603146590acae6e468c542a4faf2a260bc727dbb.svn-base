using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSupportTest
{
    [TestClass]
    public class ClientCreator
    {

        [TestMethod]
        public void CreateClientClosure()
        {
            Console.WriteLine(
            LuaSupport.Mvc.ComputercraftClientCreator.Create("LuaTestClient", new Type[] { typeof(LuaTestWebApp.Controllers.LuaTestController) })
            );
        }

        [TestMethod]
        public void CreateClientMeta()
        {
            Console.WriteLine(
            LuaSupport.Mvc.ComputercraftClientCreator.CreateMeta("LuaTestClient", new Type[] { typeof(LuaTestWebApp.Controllers.LuaTestController) })
            );
        }

        [TestMethod]
        public void CreateClient2()
        {
            Console.WriteLine(
                LuaSupport.Mvc.ComputercraftClientCreator2.Create("LuaTestClient", new Type[] { typeof(LuaTestWebApp.Controllers.LuaTestController) })
            );
        }
    }
}
