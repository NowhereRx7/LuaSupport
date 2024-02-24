using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LuaSupport.Mvc
{
    /// <summary>
    /// This class will inspect an MVC REST API controller and return all of the necessary information to create a client.
    /// </summary>
    internal sealed class ControllerInspector
    {

        private static string areaName = string.Empty;
        private static string controllerName = string.Empty;
        private static string controllerRoute = string.Empty;

        private static List<string> GetParameterNames(MethodInfo method)
        {
            List<string> ret = new();
            ParameterInfo[] parameters = method.GetParameters();
            foreach (ParameterInfo param in parameters)
                ret.Add(param.Name);
            return ret;
        }

        /// <summary>
        /// Generates a route for a method.
        /// </summary>
        /// <param name="method">
        /// <see cref="MethodInfo"/> for the method.
        /// </param>
        /// <param name="queryParameters">
        /// [Out] A collection of parameter names that were not specified in the route.
        /// </param>
        /// <returns>
        /// A string containing the method route.
        /// </returns>
        private static string GetMethodRoute(MethodInfo method, out string[] queryParameters)
        {
            List<string> parameters = GetParameterNames(method);
            string methodRoute = string.Empty;
            HttpGetAttribute get = (HttpGetAttribute)method.GetCustomAttribute(typeof(HttpGetAttribute));
            if (!(get is null))
                methodRoute = get.Template ?? String.Empty;
            HttpPostAttribute post = (HttpPostAttribute)method.GetCustomAttribute(typeof(HttpPostAttribute));
            if (!(post is null) && String.IsNullOrEmpty(methodRoute))
                methodRoute = post.Template ?? String.Empty;
            RouteAttribute route = (RouteAttribute)method.GetCustomAttribute(typeof(RouteAttribute));
            if (!(route is null))
                methodRoute = route.Template ?? string.Empty;

            if (String.IsNullOrEmpty(methodRoute) || methodRoute.StartsWith('{'))
            {
                string old = methodRoute;
                if (controllerRoute.Contains("[action]"))
                    methodRoute = controllerRoute.Replace("[action]", method.Name);
                else
                    methodRoute = method.Name;
                if (!String.IsNullOrEmpty(old))
                    methodRoute = methodRoute + "/" + old;
            }
            if (methodRoute.Contains("[controller]"))
                methodRoute = methodRoute.Replace("[controller]", controllerName);
            else
                methodRoute = controllerRoute + "/" + methodRoute;

            if (methodRoute.Contains("[area]"))
                methodRoute = methodRoute.Replace("[area]", areaName);
            else if (!String.IsNullOrEmpty(areaName) && !controllerRoute.Contains("[controller]"))  //If an Area is defined, but the controller route doesn't use it, MVC doesn't map it
                methodRoute = areaName + "/" + methodRoute;

            //UNDONE: As with the Area issue above, there could be others if the controller has some weird route; really it's just bad code in the Controller

            foreach (Match match in Regex.Matches(methodRoute, @"\{[\w\:]+\}"))
            {
                string p = match.Value.TrimStart('{').TrimEnd('}');
                if (p.Contains(':'))
                    p = p.Split(':')[0];
                if (parameters.Contains(p))
                    parameters.Remove(p);
            }

            methodRoute = methodRoute.TrimStart('/');
            queryParameters = parameters.ToArray();
            return methodRoute;
        }

        private static ControllerAction GetMethod(MethodInfo method)
        {
            System.Net.Http.HttpMethod verb;
            if (Attribute.IsDefined(method, typeof(HttpGetAttribute)))
                verb = System.Net.Http.HttpMethod.Get;
            else if (Attribute.IsDefined(method, typeof(HttpPostAttribute)))
                verb = System.Net.Http.HttpMethod.Post;
            else
                return null;
            //UNDONE: What if it has neither, then what?  REST APIs should use these attributes, but what about just regular MVC support?

            string[] queryParams;
            string route = GetMethodRoute(method, out queryParams);
            if ((verb == System.Net.Http.HttpMethod.Post) && (queryParams.Length > 1))
                throw new ArgumentException("Post method '" + route + "'  has more than one body paramater!", nameof(method));

            ControllerAction ret = new();
            ret.Area = areaName;
            ret.Controller = controllerName;
            ret.Name = method.Name;
            ret.Parameters.AddRange( method.GetParameters());
            ret.QueryParameters.AddRange(queryParams);
            ret.Route = route;
            ret.Verb = verb;
            ret.ReturnType = method.ReturnType;
            return ret;
        }

        /// <summary>
        /// Returns a collection of <see cref="ControllerAction"/> objects describing the actions available in the controller.
        /// </summary>
        /// <param name="controllerType">
        /// A <see cref="Type"/> that inherits from <see cref="ControllerBase"/>.
        /// </param>
        /// <returns></returns>
        public static IList<ControllerAction> Inspect(Type controllerType)
        {
            if (!controllerType.IsAssignableTo(typeof(ControllerBase)))
                throw new ArgumentException("Type passed does not appear to be a Controller.", nameof(controllerType));
            
            controllerName = Regex.Replace(controllerType.Name, "Controller$", String.Empty, RegexOptions.IgnoreCase);
            controllerRoute = string.Empty;

            areaName = string.Empty;
            AreaAttribute area = (AreaAttribute)controllerType.GetCustomAttribute(typeof(AreaAttribute));
            if (!(area is null))
                areaName = area.RouteValue ?? string.Empty;

            RouteAttribute route = (RouteAttribute)controllerType.GetCustomAttribute(typeof(RouteAttribute));
            if (!(route is null))
                controllerRoute = route.Template ?? string.Empty;
            if (String.IsNullOrEmpty(controllerRoute))
                controllerRoute = controllerName;

            List<ControllerAction> ret = new();
            MethodInfo[] methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (MethodInfo method in methods)
            {
                ControllerAction m = GetMethod(method);
                if (!(m is null))
                    ret.Add(m);
            }

            return ret;

        }
    }
}
