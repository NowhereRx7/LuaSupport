using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSupport
{
    /// <summary>
    /// Contains settings for configuring <see cref="LuaFormatter"/>.
    /// </summary>
    public class LuaFormatterConfig
    {
        /// <summary>
        /// Determines if __type="{GetType().ToString()}" should be output during object serialization.
        /// </summary>
        public bool SerializeType { get; set; } = true;

        /// <summary>
        /// Determines if Deserialize should check for a __type value and attempt to instantiate that object and populate properties.
        /// </summary>
        public bool DeserializeType { get; set; } = true;

        /// <summary>
        /// Determines if output should be formatted (tab, newline).
        /// </summary>
        public bool Indenting { get; set; } = false;

        /// <summary>
        /// Determines if output should be marked up with commenting on basic data types. Typed objects will only be marked up if <see cref="SerializeType"/> is <b>false</b>.
        /// <para>
        /// MarkupOutput infers <see cref="Indenting"/>=<b>true</b>.
        /// </para>
        /// <para>
        /// MarkupOutput will prevent properties that cannot be written to from being output, since they cannot be deserialized!
        /// </para>
        /// <para>
        /// This is useful for generating empty objects for use on the client side to have a model to work from.
        /// </para>
        /// </summary>
        public bool MarkupOutput { get; set; } = false;
    }
}
