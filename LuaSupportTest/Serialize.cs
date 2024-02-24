using LuaSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSupportTest
{
    [TestClass]
    public class Serialize
    {

        [TestMethod]
        public void Object()
        {
            LuaSupport.TestType o = new();
            o.Int1 = 100;
            o.String1 = "Test";
            o.Obj = new ApplicationException();
            LuaSupport.LuaFormatter luaFormatter = new();
            MemoryStream stream = new MemoryStream();
            luaFormatter.Serialize(stream, o);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(content);
            stream.Close();
        }

        [TestMethod]
        public void IndentedObject()
        {
            LuaSupport.TestType o = new();
            o.Int1 = 100;
            o.String1 = "Test";
            o.Obj = new ApplicationException();
            LuaFormatterConfig lfc = new()
            {
                Indenting = true
            };
            LuaSupport.LuaFormatter luaFormatter = new(lfc);
            MemoryStream stream = new MemoryStream();
            luaFormatter.Serialize(stream, o);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(content);
            stream.Close();
        }

        [TestMethod]
        public void MarkupObject()
        {
            LuaSupport.TestType o = new();
            //o.Int1 = 100;
            //o.String1 = "Test";
            o.Obj = new LuaSupport.TestType();
            LuaFormatterConfig lfc = new()
            {
                MarkupOutput = true
            };
            LuaSupport.LuaFormatter luaFormatter = new(lfc);
            MemoryStream stream = new MemoryStream();
            luaFormatter.Serialize(stream, o);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(content);
            stream.Close();
        }

        [TestMethod]
        public void IndentedArray()
        {
            string[] o = new string[] { "A", "B", "C" };
            LuaFormatterConfig lfc = new()
            {
                Indenting = true
            };
            LuaSupport.LuaFormatter luaFormatter = new(lfc);
            MemoryStream stream = new MemoryStream();
            luaFormatter.Serialize(stream, o);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(content);
            stream.Close();
        }

    }
}
