using System;
using System.Text;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using System.ComponentModel;

namespace LuaSupport
{
    public class LuaFormatter : IFormatProvider, ICustomFormatter, IFormatter
    {
        internal LuaFormatterConfig config = new();

        #region "IFormatProvider"

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return this;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region "ICustomFormatter"

        private static string EncodeChar(char c)
        {
            if (c == '\0') return @"\0";
            else if (c == '\b') return @"\b";
            else if (c == '\f') return @"\f";
            else if (c == '\n') return @"\n";
            else if (c == '\r') return @"\r";
            else if (c == '\t') return @"\t";
            else if (c == '\v') return @"\v";
            else if (c == '\\') return @"\\";
            else if (c == '\"') return @"\""";
            else if (c == '\'') return @"\'";
            else if (c == '[') return @"\[";
            else if (c == ']') return @"\]";
            else if (c < 0x20) return @"\" + ((int)c).ToString().PadLeft(3, '0');
            else if (c >= 0x7e && c <= 0xFF) return @"\" + ((int)c).ToString().PadLeft(3, '0');
            else if (c > 0xFF) return @"\u{" + ((int)c).ToString("x") + @"}";
            else return string.Empty + c;
        }

        public string Format(string fmt, object arg, IFormatProvider formatProvider)
        {
            if (!Equals(formatProvider))
                return null;
            if (arg is null)
            {
                return "nil";
            }
            else if (arg is bool a)
            {
                return a.ToString().ToLower();
            }
            else if (arg is DateTime b)
            {
                return Format("L", b.ToString(fmt), formatProvider);
            }
            else if (arg is char c)
            {
                StringBuilder ret = new();
                ret.Append('"');
                ret.Append(EncodeChar(c));
                ret.Append('"');
                return ret.ToString();
            }
            else if (arg is string s)
            {
                StringBuilder ret = new();
                ret.Append('"');
                for (int i = 0; i < s.Length; i++)
                {
                    ret.Append(EncodeChar(s[i]));
                }
                ret.Append('"');
                return ret.ToString();
            }
            else if (arg is IEnumerable e)
            {
                StringBuilder ret = new();
                ret.Append('{');
                foreach (var o in e)
                {
                    ret.Append(string.Format(formatProvider, "{0:L}", o) + ",");
                }
                ret.Append('}');
                return ret.ToString();
            }
            else
            {
                try
                {
                    return HandleOtherFormats(fmt, arg);
                }
                catch (FormatException ex)
                {
                    throw new FormatException(string.Format("The format of '{0}' is invalid.", fmt), ex);
                }
            }
        }

        private static string HandleOtherFormats(string fmt, object arg)
        {
            if (arg is IFormattable a)
            {
                return a.ToString(fmt, System.Globalization.CultureInfo.CurrentCulture);
            }
            else if (arg != null)
            {
                return arg.ToString();
            }
            else
            {
                return string.Empty;
            }
        }


        #endregion

        #region "IFormatter"

        private SerializationBinder _binder;
        private StreamingContext _context;
        private ISurrogateSelector _surrogateSelector;

        public LuaFormatter()
        {
            _context = new StreamingContext(StreamingContextStates.All);
        }

        public LuaFormatter(LuaFormatterConfig config) : this()
        {
            if (!(config == null)) this.config = config;
        }


        public SerializationBinder Binder { get { return _binder; } set { _binder = value; } }
        public StreamingContext Context { get { return _context; } set { _context = value; } }
        public ISurrogateSelector SurrogateSelector { get { return _surrogateSelector; } set { _surrogateSelector = value; } }


        #region "Deserialize"

        private static string ReadUntilBreak(Stream serializationStream)
        {
            string s = String.Empty;
            char c;
            int b;
            do
            {
                c = (char)(b = serializationStream.ReadByte());
                if (b != -1 && !Char.IsWhiteSpace(c) && c != ',' && c != '}')
                    s += c;
            } while (b != -1 && !Char.IsWhiteSpace(c) && c != ',' && c != '}');
            return s;
        }

        private static object DeserializeNil(Stream serializationStream)
        {
            serializationStream.Seek(-1, SeekOrigin.Current);
            long pos = serializationStream.Position;
            string s = ReadUntilBreak(serializationStream);
            if (s != "nil")
                throw new InvalidDataException("Expected nil, but read '" + s + "' at position " + pos.ToString());
            else
                return null;
        }

        private static object DeserializeNumber(Stream serializationStream)
        {
            serializationStream.Seek(-1, SeekOrigin.Current);
            long pos = serializationStream.Position;
            string s = ReadUntilBreak(serializationStream);
            try
            {
                if (s.Contains("p", StringComparison.InvariantCultureIgnoreCase))
                    throw new InvalidDataException("Unable to parse hex float notation '" + s + "' at position " + pos.ToString());
                else if (s.Contains("x", StringComparison.InvariantCultureIgnoreCase))
                    return Convert.ToInt64(s, 16);
                else if (s.Contains('.') || s.Contains('e'))
                    return Convert.ToDouble(s);
                else
                    return Convert.ToInt64(s);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Expected a number, but read '" + s + "' at position " + pos.ToString(), ex);
            }
            /*
              Examples of valid integer constants are
                 3   345   0xff   0xBEBADA
              Examples of valid float constants are
                 3.0     3.1416     314.16e-2     0.31416E1     34e1
                 0x0.1E  0xA23p-4   0X1.921FB54442D18P+1
             */
        }


        /*
        A short literal string can be delimited by matching single or double quotes, and can contain the following C-like escape sequences: '\a' (bell), '\b' (backspace), '\f' (form feed), '\n' (newline), '\r' (carriage return), '\t' (horizontal tab), 
        '\v' (vertical tab), '\\' (backslash), '\"' (quotation mark [double quote]), and '\'' (apostrophe [single quote]). A backslash followed by a line break results in a newline in the string. The escape sequence '\z' skips the following span of white-space characters, 
        including line breaks; it is particularly useful to break and indent a long literal string into multiple lines without adding the newlines and spaces into the string contents. A short literal string cannot contain unescaped line breaks nor escapes not forming a valid escape sequence.
        We can specify any byte in a short literal string by its numeric value (including embedded zeros). This can be done with the escape sequence \xXX, where XX is a sequence of exactly two hexadecimal digits, 
        or with the escape sequence \ddd, where ddd is a sequence of up to three decimal digits. (Note that if a decimal escape sequence is to be followed by a digit, it must be expressed using exactly three digits.)
        The UTF-8 encoding of a Unicode character can be inserted in a literal string with the escape sequence \u{XXX} (note the mandatory enclosing brackets), where XXX is a sequence of one or more hexadecimal digits representing the character code point. 
        */
        private static string DeserializeString(Stream serializationStream)
        {
            serializationStream.Seek(-1, SeekOrigin.Current);
            long pos = serializationStream.Position;
            char quoteChar = (char)serializationStream.ReadByte(); //Need this to determine escaping and end of string
            StringBuilder ret = new();
            while (serializationStream.Position < serializationStream.Length)
            {
                int b;
                char c = (char)(b = serializationStream.ReadByte());
                if (b == -1)
                    break;
                else if (c == quoteChar)
                    return ret.ToString();
                else if (c == '\\')
                {
                    c = (char)(b = serializationStream.ReadByte());
                    if (b == -1)
                        break;
                    else if (c == '\0') ret.Append('\0');
                    else if (c == '\a') ret.Append('\a');
                    else if (c == '\b') ret.Append('\b');
                    else if (c == '\f') ret.Append('\f');
                    else if (c == '\n') ret.Append('\n');
                    else if (c == '\r') ret.Append('\r');
                    else if (c == '\t') ret.Append('\t');
                    else if (c == '\v') ret.Append('\v');
                    else if (c == '\\') ret.Append('\\');
                    else if (c == '\'') ret.Append('\'');
                    else if (c == '\"') ret.Append('\"');
                    else if (Char.IsNumber(c)) // 000 int char
                    {
                        string s = string.Empty + c;
                        for (int i = 0; i < 2; i++)
                        {
                            c = (char)(b = serializationStream.ReadByte());
                            if (b == -1)
                                throw new InvalidDataException("Unexpected end of stream.");
                            if (!Char.IsNumber(c))
                                throw new InvalidDataException("Encountered unexpected non-numeric character '" + c + "' at position " + (serializationStream.Position - 1).ToString());
                            s += c;
                        }
                        ret.Append(Convert.ToChar(int.Parse(s)));
                    }
                    else if (c == 'x') // xFF hex char
                    {
                        string s = string.Empty;
                        for (int i = 0; i < 2; i++)
                        {
                            c = (char)(b = serializationStream.ReadByte());
                            if (b == -1)
                                throw new InvalidDataException("Unexpected end of stream.");
                            else if (!Char.IsNumber(c) && (c < 'A' || c > 'F'))
                                throw new InvalidDataException("Encountered unexpected non-hex character '" + c + "' at position " + (serializationStream.Position - 1).ToString());
                            s += c;
                        }
                        ret.Append(Convert.ToChar(Convert.ToByte(s, 16)));
                    }
                    else if (c == 'u') // u{FFF} hex unicode char
                    {
                        c = (char)(b = serializationStream.ReadByte());
                        if (b == -1)
                            break;
                        else if (c != '{')
                            throw new InvalidDataException("Encountered unexpected character '" + c + "' at position " + (serializationStream.Position - 1).ToString() + "; expected }.");
                        string s = string.Empty;
                        while (c != '}')
                        {
                            c = (char)(b = serializationStream.ReadByte());
                            if (b == -1)
                                throw new InvalidDataException("Unexpected end of stream.");
                            else if (!Char.IsNumber(c) && (c != '}') && (c < 'A' || c > 'F'))
                                throw new InvalidDataException("Encountered unexpected non-hex character '" + c + "' at position " + (serializationStream.Position - 1).ToString());
                            else if (c != '}')
                                s += c;
                        }
                        ret.Append(Convert.ToChar(Convert.ToInt32(s, 16)));
                    }
                    else
                        throw new InvalidDataException("Invalid escape character '" + c + "' in string encountered at position " + (serializationStream.Position - 1).ToString());
                    //UNDONE: Not sure if \z is needed; skips subsequent whitespace
                }
                else
                    ret.Append(c);

            }
            throw new InvalidDataException("Unexpected end of stream.");
        }

        /*
        Literal strings can also be defined using a long format enclosed by long brackets. We define an opening long bracket of level n as an opening square bracket followed by n equal signs followed by another opening square bracket.
        So, an opening long bracket of level 0 is written as [[, an opening long bracket of level 1 is written as [=[, and so on. A closing long bracket is defined similarly; for instance, a closing long bracket of level 4 is written as ]====].
        A long literal starts with an opening long bracket of any level and ends at the first closing long bracket of the same level. It can contain any text except a closing bracket of the same level.
        Literals in this bracketed form can run for several lines, 
        
        do not interpret any escape sequences, 
        
        and ignore long brackets of any other level.
        Any kind of end-of-line sequence (carriage return, newline, carriage return followed by newline, or newline followed by carriage return) is converted to a simple newline.
        */
        private static string DeserializeBlockString(Stream serializationStream)
        {
            throw new NotImplementedException("Cannot deserialize a block string... yet.");
            //UNDONE: Deserialize block strings; even though they are probably never created with a serializer
            //Actually, this should never be encountered with serialized data and would only need to be handled if parsing Lua code

            //This may need implementing to handle block comments, if I serialize any
        }

        private static string DeserializeComment(Stream serializationStream)
        {
            int b;
            char c = (char)(b = serializationStream.ReadByte());
            if (b == -1)
                throw new InvalidDataException("Unexpected end of stream.");
            if (c != '-')
                throw new InvalidDataException("Expected comment start, but encountered character '" + c + "' at position " + (serializationStream.Position - 1).ToString());
            c = (char)(b = serializationStream.ReadByte());
            if (b == -1)
                throw new InvalidDataException("Unexpected end of stream.");
            if (c == '[')
                throw new NotImplementedException("Unable to deserialize block comments yet.");
            //UNDONE: Handle block comments, but new need DeseralizeBlockString implemented to eat the data!
            while (b != -1 && c != '\n')
            {
                c = (char)(b = serializationStream.ReadByte());
            }
            return null;
            // -- Short comment intil EOL or EOS
            // --[[
            // Long comment - multiple lines
            // --]]
            // Long comments can contain levels like strings [=[ {level 1} ]=]
            // I've also seen long comments showing ]] without -- at the end, which I think is also correct, since whitespace is ignored
            // According to the Lua documentation, I believe ]] can appear on the same line, or anywhere, since technically whitespace is ignored; except single-line comments, which do indeed expect a newline
            // e.g. --[[ This is a single-line block comment ]]
            // However, standard block comments cannot be nested; this results in an error, which is good

        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        private object TableToTypedObject(Dictionary<object, object> table)
        {
            Type t = GetType((string)table["__type"]);
            if (!(t is null))
            {
                table.Remove("__type");
                if (t.IsArray)
                {
                    if (t.GetArrayRank() > 1)
                    {
                        //UNDONE: Multi-dimensional arrays; Lua doesn't have them, so do I even care to implement them
                        /*
                            var indices = new[] { 2, 3 };                
                            var arr = Array.CreateInstance(typeof(int), indices);
                            var value = 1;
                            for (int i = 0; i < indices[0]; i++)
                            {
                                for (int j = 0; j < indices[1]; j++)
                                {
                                    arr.SetValue(value++, new[] { i, j });
                                }
                            }

                            //arr = [ [ 1, 2, 3 ], [ 4, 5, 6 ] ]
                        */
                        throw new NotImplementedException("Multi-dimensional arrays are not yet supported - and don't exist in native Lua anyway.");
                    }
                    Type elementType = t.GetElementType();
                    ConstructorInfo ci = t.GetConstructor(new Type[] { typeof(Int32) });
                    if (!(ci is null))
                    {
                        var o = ci.Invoke(new object[] { table.Count });
                        var i = 0;
                        foreach (var val in table.Values)
                        {
                            var newVal = Convert.ChangeType(val, elementType);
                            ((Array)o).SetValue(newVal, i);
                            i++;
                        }
                        return o;
                    }
                }
                else
                {
                    object ret = null;
                    ConstructorInfo ci = t.GetConstructor(System.Type.EmptyTypes);
                    if (ci is null)
                    {
                        ret = FormatterServices.GetUninitializedObject(t);
                    }
                    else
                    {
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                        {
                            ret = ci.Invoke(null);
                            foreach (var key in table.Keys)
                            {
                                var newKey = Convert.ChangeType(key, t.GenericTypeArguments[0]);
                                var newVal = Convert.ChangeType(table[key], t.GenericTypeArguments[1]);
                                ((IDictionary)ret).Add(newKey, newVal);
                            }
                        }
                        else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            ret = ci.Invoke(null);
                            foreach (var val in table.Values)
                            {
                                var newVal = Convert.ChangeType(val, t.GenericTypeArguments[0]);
                                ((IList)ret).Add(newVal);
                            }
                        }
                        else //Complex types
                        {
                            try
                            {
                                //UNDONE: I don't know if I should be using GetUninitializedObject or not.
                                // It bypasses default constructor, which can be good in some cases.
                                // Some deserializers use it, some don't
                                ret = FormatterServices.GetUninitializedObject(t);
                            }
                            catch
                            {
                                ret = ci.Invoke(null);
                            }
                            PropertyInfo[] propertyInfos = t.GetProperties();
                            foreach (PropertyInfo pi in propertyInfos)
                            {
                                if (table.ContainsKey(pi.Name) && pi.CanWrite)
                                    pi.SetValue(ret, table[pi.Name]);
                            }
                        }
                        return ret;
                    }
                }
            }
            return null;
        }

        private object DeserializeTable(Stream serializationStream)
        {
            long ikey = 1;
            long xkey = 0;
            Dictionary<Object, Object> ret = new();
            char c;
            int b;
            do
            {
                c = (char)(b = serializationStream.ReadByte());
                while (b != -1 && (Char.IsWhiteSpace(c) || c == ','))
                {
                    c = (char)(b = serializationStream.ReadByte());
                }
                if (b == -1)
                    throw new InvalidDataException("Unexpected end of stream.");
                else if (c == '}')
                    break;
                else if (Char.IsNumber(c) || c == '\'' || c == '\"') //Value
                {
                    serializationStream.Seek(-1, SeekOrigin.Current);
                    object val = DeserializeValue(serializationStream);
                    ret.Add(ikey++, val);
                }
                else if (c == '-') // Comment
                    DeserializeComment(serializationStream);
                else if (c == '{') // Table
                {
                    object val = DeserializeTable(serializationStream);
                    ret.Add(ikey++, val);
                }
                else // Key
                {
                    string key = string.Empty;
                    long pos = serializationStream.Position - 1;
                    key += c;
                    do
                    {
                        c = (char)(b = serializationStream.ReadByte());
                        if (b == -1)
                            throw new InvalidDataException("Unexpected end of stream.");
                        else if ((c != '=') && (c != '}'))
                            key += c;
                    } while ((c != '=') && (c != '}'));
                    //HACK: I added '}" to deal with a bug, but why isn't whitespace being properly handled after a value trailing comma?  Why are we even in key?  Need to debug!!!
                    key = key.Trim();
                    if (key.StartsWith('[')) //Numeric or encoded string
                    {
                        key = key.TrimStart('[').TrimEnd(']').Trim();
                        if (key.StartsWith("\'") || key.StartsWith("\""))
                        {
                            if (!key.EndsWith("\'") && !key.EndsWith("\""))
                                throw new InvalidDataException("Expected string key and read '" + key + "' at position " + pos.ToString());
                            using MemoryStream ms = new();
                            {
                                ms.Write(UTF8Encoding.UTF8.GetBytes(key.ToCharArray()));
                                ms.Seek(1, SeekOrigin.Begin);
                                try
                                {
                                    key = DeserializeString(ms);
                                }
                                catch (Exception ex)
                                {
                                    throw new InvalidDataException("Expected string key and read '" + key + "' at position " + pos.ToString(), ex);
                                }
                            }
                        }
                        else if (!long.TryParse(key, out xkey))
                            throw new InvalidDataException("Expected numeric key and read '" + key + "' at position " + pos.ToString());
                    }
                    if (!String.IsNullOrEmpty(key))
                    {
                        object val = DeserializeValue(serializationStream);
                        if (xkey > 0)
                        {
                            ret.Add(xkey, val);
                        }
                        else
                            ret.Add(key, val);
                        xkey = 0;
                    }
                }
            } while (c != '}');
            if (config.DeserializeType && ret.ContainsKey("__type") && ret["__type"] is string)
            {
                object o = TableToTypedObject(ret);
                if (o is not null)
                    return o;
            }
            return ret;
        }

        private object DeserializeValue(Stream serializationStream)
        {
            int b;
            char c = (char)(b = serializationStream.ReadByte());
            while (b != -1 && Char.IsWhiteSpace(c))
            {
                c = (char)(b = serializationStream.ReadByte());
            }
            if (b == -1)
                throw new InvalidDataException("Unexpected end of stream.");
            else if (c == 'n')
                return DeserializeNil(serializationStream);
            else if (Char.IsNumber(c))
                return DeserializeNumber(serializationStream);
            else if (c == '\'' || c == '\"')
                return DeserializeString(serializationStream);
            else if (c == '-')
            {
                //UNDONE: Should top-level comments be allowed, or only in objects?  Also, this is untested.
                DeserializeComment(serializationStream);
                return DeserializeValue(serializationStream);
            }
            else if (c == '[')
                return DeserializeBlockString(serializationStream);
            else if (c == '{')
                return DeserializeTable(serializationStream);
            else
                throw new InvalidDataException("And invalid character was encountered during deserialization at postion " + (serializationStream.Position - 1).ToString());
        }

        public object Deserialize(Stream serializationStream)
        {
            if (serializationStream.Length == 0)
                return null;
            serializationStream.Seek(0, SeekOrigin.Begin);
            return DeserializeValue(serializationStream);
        }

        public object Deserialize(Stream serializationStream, Type t)
        {
            //TODO: I know this typed deserialization will need some testing and work.
            //UNDONE: What about nested types?
            Object obj = DeserializeValue(serializationStream);
            if (obj.GetType() == t)
            {
                return obj;
            }
            else if (obj is Dictionary<Object, Object>)
            {
                ((Dictionary<Object, Object>)obj)["__type"] = (t.FullName);
                object o = TableToTypedObject((Dictionary<Object, Object>)obj);
                if (o is not null)
                    return o;
                else
                    throw new InvalidCastException("Type conversion failed.");
            }
            else if (obj.GetType().IsPrimitive)
                return obj;
            else
            {
                throw new InvalidCastException("The serialized type did not match the expected output type.");
            }

        }

        #endregion

        #region "Serialize"

        private static readonly string[] keywords = new string[] { "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto", "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while" };
        private static bool IsKeyword(string word)
        {
            word = word.ToLower();
            foreach (string key in keywords)
            {
                if (word == key)
                    return true;
            }
            return false;
        }

        private void WriteTableOpening(StreamWriter sw, Type type, ref int level)
        {
            sw.Write('{');
            if (config.Indenting || config.MarkupOutput)
            {
                sw.Write("\n");
                level++;
            }
            if (config.SerializeType)
            {
                sw.Write(new String('\t', level));
                sw.Write("__type=\"" + type.ToString() + "\", ");
                if (config.Indenting || config.MarkupOutput) sw.Write("\n");
            }
        }

        private void WriteTableClosing(StreamWriter sw, ref int level)
        {
            if (config.Indenting || config.MarkupOutput)
            {
                level--;
                sw.Write(new String('\t', level));
            }
            sw.Write('}');
        }

        private void Serialize(Stream serializationStream, object graph, int level)
        {
            StreamWriter sw = new StreamWriter(serializationStream) { AutoFlush = true };
            if (graph is null)
            {
                sw.Write("nil");
            }
            else if ((graph is string) || (graph is char))
            {
                sw.Write(Format("", graph, this));
            }
            else if (graph.GetType().IsPrimitive)
            {
                sw.Write(Format("", graph, this));
            }
            else if (graph is IDictionary dict)
            {
                WriteTableOpening(sw, graph.GetType(), ref level);
                foreach (DictionaryEntry entry in dict)
                {
                    if (entry.GetType().IsSerializable)
                    {
                        sw.Write(new String('\t', level));
                        sw.Write("[" + Format("", entry.Key, this) + "]=");
                        Serialize(serializationStream, entry.Value);
                        if (config.Indenting || config.MarkupOutput)
                            sw.Write(",\n");
                        else
                            sw.Write(", ");
                    }
                }
                WriteTableClosing(sw, ref level);

            }
            else if (graph.GetType().IsArray && graph.GetType().GetArrayRank() > 1)
            {
                //UNDONE: Multi-dimensional arrays
                // What would these look like, an array of array of arrays?
                throw new NotImplementedException("Multi-dimensional arrays are not yet supported - and don't exist in native Lua.");
            }
            else if (graph is IEnumerable)
            {
                WriteTableOpening(sw, graph.GetType(), ref level);
                foreach (var item in (IEnumerable)graph)
                {
                    if (item.GetType().IsSerializable)
                    {
                        sw.Write(new String('\t', level));
                        Serialize(serializationStream, item);
                        if (config.Indenting || config.MarkupOutput)
                            sw.Write(",\n");
                        else
                            sw.Write(", ");
                    }
                }
                WriteTableClosing(sw, ref level);
            }
            else // Object
            {
                WriteTableOpening(sw, graph.GetType(), ref level);
                PropertyInfo[] props = graph.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.CanRead &&
                    (config.MarkupOutput ? prop.CanWrite : true) &&
                    !Attribute.IsDefined(prop, typeof(IgnoreDataMemberAttribute)) &&
                    !Attribute.IsDefined(prop, typeof(System.Xml.Serialization.SoapIgnoreAttribute)) &&
                    !Attribute.IsDefined(prop, typeof(System.Xml.Serialization.XmlIgnoreAttribute)) &&
                    !Attribute.IsDefined(prop, typeof(System.Text.Json.Serialization.JsonIgnoreAttribute)))
                    {
                        sw.Write(new String('\t', level));
                        object value = prop.GetValue(graph, null);
                        if (config.MarkupOutput)
                        {
                            sw.Write("-- " + prop.PropertyType.ToString() + "\n");
                            sw.Write(new String('\t', level));
                        }
                        if (value is null)
                        {
                            if (IsKeyword(prop.Name))
                                sw.Write("[" + Format("", prop.Name, this) + "]=nil");
                            else
                                sw.Write(prop.Name + "=nil");
                        }
                        //UNDONE: Do I or do I not want to abide by IsSerializable?  I don't at top level and it caught me here!
                        else //if (value.GetType().IsSerializable)
                        {

                            if (IsKeyword(prop.Name))
                                sw.Write("[" + Format("", prop.Name, this) + "]=");
                            else
                                sw.Write(prop.Name + "=");
                            Serialize(serializationStream, value, level);

                        }
                        if (config.Indenting || config.MarkupOutput)
                            sw.Write(",\n");
                        else
                            sw.Write(", ");
                    }
                }
                WriteTableClosing(sw, ref level);
            }
            sw.Flush();
        }

        public void Serialize(Stream serializationStream, object graph)
        {
            this.Serialize(serializationStream, graph, 0);
        }

        #endregion

        #endregion
    }
}