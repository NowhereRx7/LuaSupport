using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace LuaTestWebApp.Controllers
{
    [ApiController]
    //[Area("Test")]
    [Route("[controller]/[action]")]
    public class LuaTestController : Controller

    {
        //Cannot get default route to work :(
        [HttpGet]
        [Route("")]
        public  String Index()
        {
            return "Test";
        }

        [HttpGet]
        public String @String()
        {
            return "This is a string";
        }

        [HttpGet]
        public int @Int()
        {
            return 100;
        }

        [HttpGet]
        public int[] IntArray()
        {
            return new int[] { 1, 2, 3, 4 };
        }

        [HttpGet]
        public string[] StringArray()
        {
            return new string[] { "a", "b" };
        }

        [HttpGet]
        public Dictionary<String, String> DictString()
        {
            Dictionary<String, String> ret = new Dictionary<String, String>();
            ret.Add("a", "Hello");
            return ret;
        }

        [HttpGet]
        public Object @Object()
        {
            Exception ex2 = new Exception("This is an inner exception");
            Exception ex = new ApplicationException("This is the message", ex2);
            return ex;
        }

        [HttpGet]
        public string EchoString(string str) //This is so bad; don't exceed max query string length
        {
            return str;
        }

        [HttpPost]
        public string EchoStringPost([FromBody] string str)
        {
            return str;
        }

        [HttpPost]
        public LuaSupport.TestType EchoTestType([FromBody] LuaSupport.TestType testType)
        {
            return testType;
        }

        [HttpPost]
        public object Echo([FromBody] object data)
        {
            return data;
        }

        [HttpPost("{i:int}")]
        public object EchoWithParam(int i, [FromBody] object data)
        {
            return data;
        }

        [HttpGet]
        public ContentResult Client()
        {
            return Content( LuaSupport.Mvc.ComputercraftClientCreator2.Create("LuaTestClient", new Type[] { this.GetType() }));
        }

    }
}
