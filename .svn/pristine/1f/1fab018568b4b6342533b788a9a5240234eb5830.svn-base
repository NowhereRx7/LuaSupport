using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using LuaSupport;

namespace LuaSupportTest
{
    [TestClass]
    public class Controller //: IClassFixture<WebApplicationFactory<LuaTestWebApp.Startup>>
    {

        private static WebApplicationFactory<LuaTestWebApp.Program> _factory;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _factory = new WebApplicationFactory<LuaTestWebApp.Program>();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _factory.Dispose();
        }

        //public Controller(WebApplicationFactory<LuaTestWebApp.Startup> factory)
        //{
        //    _factory = factory;
        //}

        [TestMethod]
        public async Task GetObject()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("luatest/object");
            response.EnsureSuccessStatusCode();
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK, "Status code not 200");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task EchoString()
        {
            var client = _factory.CreateClient();
            StringContent content = new("\"Test string\"");
            var response = await client.PostAsync("luatest/echo", content);
            response.EnsureSuccessStatusCode();
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK, "Status code not 200");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task EchoStringPost()
        {
            var client = _factory.CreateClient();
            StringContent content = new("\"Test string\"");
            var response = await client.PostAsync("luatest/echostringpost", content);
            response.EnsureSuccessStatusCode();
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK, "Status code not 200");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        private async Task Echo(object o)
        {
            var client = _factory.CreateClient();
            LuaFormatter lf = new();
            StringContent content;
            using (var ms = new System.IO.MemoryStream())
            {
                lf.Serialize(ms, o);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()));
            }
            Console.WriteLine("Sent: {0}", await content.ReadAsStringAsync());
            var response = await client.PostAsync("luatest/echo", content);
            response.EnsureSuccessStatusCode();
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK, "Status code not 200");
            string output = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Rcvd: {0}", output);
            Assert.IsTrue(output == (await content.ReadAsStringAsync()), "Echo did not match");
        }

        [TestMethod]
        public async Task EchoObject()
        {
            var o = new LuaSupport.TestType();
            o.Int1 = 1234;
            o.String1 = "TestType";
            await Echo(o);
        }

        [TestMethod]
        public async Task EchoDictionary()
        {
            var o = new Dictionary<int, string>();
            o.Add(1, "1");
            o.Add(2, "2");
            await Echo(o);
        }

        [TestMethod]
        public async Task EchoArray()
        {
            string[] o = new string[] { "A", "B", "C" };
            await Echo(o);
        }

    }
}
