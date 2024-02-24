using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LuaSupportTest
{
    [TestClass]
    public class Deserialize
    {
        [TestMethod]
        public void DeserializeWholeNumber()
        {
            const string data = "12345";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Int64), "o is not Int64");
            Assert.IsTrue((Int64)o == 12345, "o != 12345");
        }

        [TestMethod]
        public void DeserializeHex()
        {
            const string data = "0xff";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Int64), "o is not Int64");
            Assert.IsTrue((Int64)o == 0xff, "o != 0xff");
        }


        [TestMethod]
        public void DeserializeDecimal()
        {
            const string data = "1.2345";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Double), "o is not Double");
            Assert.IsTrue((double)o == 1.2345, "o != 1.2345");
        }

        [TestMethod]
        public void DeserializeDouble()
        {
            const string data = "3.0";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Double), "o is not Double");
            Assert.IsTrue((double)o == 3.0, "o != 3.0");
        }

        [TestMethod]
        public void DeserializeNotation()
        {
            const string data = "314.16e-2";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Double), "o is not Double");
            Assert.IsTrue((double)o == 3.1416, "o != 3.1416");
        }


        [TestMethod]
        public void DeserializeNotation2()
        {
            const string data = "0.31416E1";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Double), "o is not Double");
            Assert.IsTrue((double)o == 3.1416, "o != 3.1416");
        }

        //[TestMethod]
        //public void DeserializeNotationHex()
        //{
        //    //There is no parsing support for hex floats, I'd have to write my own
        //    const string data = "0XA23p-4";
        //}

        [TestMethod]
        public void DeserializeString1()
        {
            const string data = "\"This is a string\"";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(String), "o is not String");
            Assert.IsTrue((String)o == "This is a string", "o != \"This is a string\"");
        }

        [TestMethod]
        public void DeserializeTable1()
        {
            const string data = "{ a = 1, 1.1, [\"aa\\'a\"]=\"aa\\'a\", {a=\"a\"} }";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Dictionary<object, object>), "o is not Dictionary");
            //Assert.IsTrue((String)o == "This is a string", "o != \"This is a string\"");
            Dictionary<object, object> d = (Dictionary<object, object>)o;
            foreach (object key in d.Keys)
            {
                Console.Write(key.ToString() + "=");
                Console.WriteLine("(" + d[key].GetType().ToString() + ")" + d[key]);
            }
        }

        [TestMethod]
        public void DeserializeTypedTable1()
        {
            const string data = "{ __type = \"LuaSupport.TestType\", Int1 = 1, Discard = \"discard\", String1 = \"String1\", }";
            LuaSupport.LuaFormatter luaFormatter = new();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(LuaSupport.TestType), "o is not TestType");
            LuaSupport.TestType tt = (LuaSupport.TestType)o;
            Assert.IsTrue(tt.Int1 == 1, "TestType.Int1 != 1");
            Assert.IsTrue(tt.String1 == "String1", "TestType.String1 != 'String1'");
        }

        [TestMethod]
        public void DeserializeStringArray()
        {
            string[] array = new string[] { "A", "B", "C" };
            LuaSupport.LuaFormatter luaFormatter = new();
            MemoryStream stream = new MemoryStream();
            luaFormatter.Serialize(stream, array);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(content);
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Array), "o is not Array");
            Assert.IsTrue(array.GetType() == o.GetType(), "GetType o != array");
            Assert.IsTrue(array.Length == ((Array)o).Length, "array.Length != o.Length");
            for (var i = 0; i < array.Length; i++)
            {
                Assert.IsTrue(array[i] == (String)((Array)o).GetValue(i), "array[" + i.ToString() + "] != o[" + i.ToString() + "]");
                Console.WriteLine("\"{0}\"=\"{1}\"", array[i], ((Array)o).GetValue(i));
            }
        }

        [TestMethod]
        public void TestDictionary()
        {
            var dict = new Dictionary<int, string>();
            dict.Add(1, "1");
            dict.Add(2, "2");
            LuaSupport.LuaFormatter luaFormatter = new();
            MemoryStream stream = new MemoryStream();
            luaFormatter.Serialize(stream, dict);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(content);
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(Dictionary<int, string>), "o is not Dictionary");
            Assert.IsTrue(dict.GetType() == o.GetType(), "GetType o != dict");
            Assert.IsTrue(dict.Keys.Count == ((Dictionary<int, string>)o).Keys.Count, "dict.Length != o.Length");
        }

        [TestMethod]
        public void TestList()
        {
            var list = new List<string>(new string[] { "A", "B", "C", "D" });
            LuaSupport.LuaFormatter luaFormatter = new();
            MemoryStream stream = new MemoryStream();
            luaFormatter.Serialize(stream, list);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(content);
            Object o = luaFormatter.Deserialize(stream);
            Assert.IsInstanceOfType(o, typeof(List<string>), "o is not List");
            Assert.IsTrue(list.GetType() == o.GetType(), "GetType o != list");
            Assert.IsTrue(list.Count == ((List<string>)o).Count, "list.Length != o.Length");
            Console.WriteLine(String.Join(",", (List<String>)o));
        }

    }
}

/*
            Examples of valid integer constants are
               3   345   0xff   0xBEBADA
            Examples of valid float constants are
               3.0     3.1416     314.16e-2     0.31416E1     34e1
               0x0.1E  0xA23p-4   0X1.921FB54442D18P+1
           */
