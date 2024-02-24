using System;
using System.Collections.Generic;
using System.Reflection;

namespace LuaSupport.Mvc
{

    /// <summary>
    /// Contains information about a single controller action.
    /// </summary>
    internal class ControllerAction
    {
        /// <summary>
        /// Name of the Area.
        /// </summary>
        public string Area { get; set; } = String.Empty;

        /// <summary>
        /// Name of the Controller, minus trailing Controller.
        /// </summary>
        public string Controller { get; set; } = String.Empty;

        /// <summary>
        /// Name of the method.
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// The HTTP method (GET/POST) that should be used to call the method.
        /// </summary>
        public System.Net.Http.HttpMethod Verb { get; set; }

        /// <summary>
        /// Routing url for the method, with variables still encoded in braces {}.
        /// </summary>
        public string Route { get; set; } = String.Empty;

        /// <summary>
        /// Parameters the method takes, that need to be in the function definition.
        /// </summary>
        public List<ParameterInfo> Parameters { get; } = new List<ParameterInfo>();

        /// <summary>
        /// Parameters that are not in the route and must be in the query string or post data.
        /// </summary>
        public List<string> QueryParameters { get; } = new List<string>();

        /// <summary>
        /// Return type for the action method.
        /// </summary>
        public Type ReturnType { get; set; }

    }
}
