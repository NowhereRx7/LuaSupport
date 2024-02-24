using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LuaSupport.Mvc
{
    /// <summary>
    /// Creates a Computercraft compatible client for MVC REST API controllers.
    /// </summary>
    public class ComputercraftClientCreator
    {

        private static string className = string.Empty;

//TODO: Fix indenting.  Probably a single tab char at the start that can be replaced by appropriate spaces.
        private const string doGet = @"
      local url = self.url .. query
      local headers = {['Accept'] = ""application/x-lua, text/x-lua, text/plain'""}
      local h = http.get(url, headers)
      if (h == nil) then 
        error(""Failed to get a valid response during GET call."", 2)
      elseif (h.getResponseCode() ~= 200) then
        error(""Response code was: ""..tostring(h.getResponseCode()), 2)
      else
        local text = h.readAll()
        if (text == """") then
          return nil
        else
          return textutils.unserialize(text)
        end
      end
    end";

        private const string doPost = @"
      local url = self.url .. query
      local headers = {['Accept'] = ""application/x-lua, text/x-lua, text/plain'"", ['content-type'] = ""application/x-lua""}
      local h = http.post(url, body, headers)
      if (h == nil) then 
        error(""Failed to get a valid response during POST call."", 2)
      elseif (h.getResponseCode() ~= 200) then 
        error(""Response code was: ""..tostring(h.getResponseCode()), 2)
      else
        local text = h.readAll()
        if (text == """") then
          return nil
        else
          return textutils.unserialize(text)
        end
      end
    end";

        private static string Comments(ControllerAction action)
        {
            //UNDONE: What about complex types as parameters or returns; how do we want to document them?  Also, do we want to create base objects for them?
            //UNDONE: Document actions and make a Help(name) function?
            StringBuilder ret = new();
            ret.AppendLine("-- Route: " + action.Route);
            if (action.Parameters.Count > 0)
            {
                ret.AppendLine("-- Parameters: ");
                foreach (ParameterInfo pi in action.Parameters)
                    ret.AppendLine("--   " + pi.Name + " [" + pi.ParameterType.ToString() + "]");
            }
            ret.AppendLine("-- Returns: " + action.ReturnType.ToString());
            return ret.ToString();
        }

        private static string ReplaceRouteTokens(string route)
        {
            string ret = route;
            foreach (Match match in Regex.Matches(route, @"\{[\w\:]+\}"))
            {
                string p = match.Value.TrimStart('{').TrimEnd('}');
                if (p.Contains(':'))
                    p = p.Split(':')[0];
                string rep = "\"..textutils.urlEncode(tostring(" + p + "))..\"";
                ret = ret.Replace(match.Value, rep);
            }
            return ret;
        }

        private static string GetBody(ControllerAction action)
        {
            //HACK: How much of this can be moved to doGet if we just pass it two tables of named parameters and values?  The less code here, the better!
            // A single named key table would not work with nil values.
            // Also, my MVC one keeps a table of methods and parameters, simplifying the base function call greatly.  Maybe take another look at that one.
            StringBuilder ret = new();
            ret.AppendLine("    local query=\"" + ReplaceRouteTokens(action.Route) + "\"");
            if (action.QueryParameters.Count > 0)
            {
                ret.AppendLine("    local subquery=\"\"");
                foreach (string queryParam in action.QueryParameters)
                {
                    ret.AppendLine("    if (" + queryParam + " ~= nil) then subquery=subquery .. \"&" + queryParam + "=\" .. textutils.urlEncode(tostring(" + queryParam + ")) end");
                }
                ret.AppendLine("    if (subquery ~= \"\") then");
                ret.AppendLine("      subquery = \"?\"..subquery:sub(2)");
                ret.AppendLine("      query = query..subquery");
                ret.AppendLine("    end");
            }
            ret.AppendLine("    return self:doGet(query)");
            return ret.ToString();
        }

        private static string PostBody(ControllerAction action)
        {
            StringBuilder ret = new();
            ret.AppendLine("    local query=\"" + ReplaceRouteTokens(action.Route) + "\"");
            if (action.QueryParameters.Count == 0)
                ret.AppendLine("    local body=nil");
            else
                ret.AppendLine("    local body=textutils.serialize(" + action.QueryParameters[0] + ")");

            ret.AppendLine("    return self:doPost(query, body)");
            return ret.ToString();
        }

        private static string ActionName(ControllerAction action)
        {
            StringBuilder ret = new();
            if (!String.IsNullOrEmpty(action.Area))
                ret.Append(action.Area + "_");
            ret.Append(action.Controller + "_");
            ret.Append(action.Name);
            return ret.ToString();
        }

        private static string FunctionSigClosure(ControllerAction action)
        {
            StringBuilder ret = new();
            //UNDONE: How to best do commenting with closures
            ret.Append(" local ");
            ret.Append(ActionName(action));
            ret.Append(" = function(");
            List<string> args = new();
            foreach (ParameterInfo pi in action.Parameters)
                args.Add(pi.Name);
            ret.Append(string.Join(", ", args.ToArray()));
            ret.Append(')');
            return ret.ToString();
        }

        private static string MakeFunctionClosure(ControllerAction action)
        {
            StringBuilder ret = new();
            ret.Append(Comments(action));
            ret.AppendLine(FunctionSigClosure(action));
            if (action.Verb == System.Net.Http.HttpMethod.Get)
                ret.Append(GetBody(action));
            if (action.Verb == System.Net.Http.HttpMethod.Post)
                ret.Append(PostBody(action));
            ret.Append("end");
            return ret.ToString();
        }

         //http://www.lua.org/pil/16.4.html  Privacy (Closure classes)
         
        public static string Create(string name, Type[] controllerTypes)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (!Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9_]*$"))
                throw new ArgumentException("Name must follow standard naming conventions for Lua variables.", nameof(name));

            className = name;
            StringBuilder ret = new();
            ret.AppendLine("function " + name + "(url)");
            ret.AppendLine(@"  if (url == nil) or (type(url) ~= ""string"") or (url == """") then
    error(""Url expects a string"", 2)
  end
  if (url:sub(-1)~=""/"") then
    url = url..""/""
  end");
            ret.AppendLine("  local self = {url = \"\",");
            ret.AppendLine("    doGet = function(self, query)" + doGet + ",");
            ret.AppendLine("    doPost = function(self, query, body)" + doPost + ",");
            ret.AppendLine("  }");
            foreach (Type controller in controllerTypes)
            {
                List<ControllerAction> actions = new(ControllerInspector.Inspect(controller));
                foreach (ControllerAction action in actions)
                    ret.AppendLine(MakeFunctionClosure(action) + "\n");
            }
            ret.AppendLine("  self.url = url");
            ret.AppendLine("  return {");
            foreach (Type controller in controllerTypes)
            {
                List<ControllerAction> actions = new(ControllerInspector.Inspect(controller));
                foreach (ControllerAction action in actions)
                    ret.AppendLine("    " + ActionName(action) + " = " + ActionName(action) + ",");
            }
            ret.AppendLine("  }");
            ret.AppendLine("end");
            ret.AppendLine();
            ret.AppendLine("--Usage:");
            ret.AppendLine("--  myClient = " + name + "(\"http://example.com/app/\")");
            ret.AppendLine("--  result = myClient.Area_Controller_Action(arg1, arg2)");
            return ret.ToString();
        }

#region "Metatable"

        private static string FunctionSigMeta(ControllerAction action)
        {
            StringBuilder ret = new();
            ret.Append("function " + className + ":");
            ret.Append(ActionName(action));
            ret.Append('(');
            List<string> args = new();
            foreach (ParameterInfo pi in action.Parameters)
                args.Add(pi.Name);
            ret.Append(string.Join(", ", args.ToArray()));
            ret.Append(')');
            return ret.ToString();
        }

        private static string MakeFunctionMeta(ControllerAction action)
        {
            StringBuilder ret = new();
            ret.Append(Comments(action));
            ret.AppendLine(FunctionSigMeta(action));
            if (action.Verb == System.Net.Http.HttpMethod.Get)
                ret.Append(GetBody(action));
            if (action.Verb == System.Net.Http.HttpMethod.Post)
                ret.Append(PostBody(action));
            ret.Append("end");
            return ret.ToString();
        }

        public static string CreateMeta(string name, Type[] controllerTypes)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (!Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9_]*$"))
                throw new ArgumentException("Name must follow standard naming conventions for Lua variables.", nameof(name));

            className = name;
            StringBuilder ret = new();
            ret.AppendLine(name + " = { ");
            ret.AppendLine("  url = \"\",");
            ret.AppendLine("}");
            ret.Append(name + ".__index = " + name);
            ret.AppendLine();
            ret.AppendLine("function " + name + ":doGet(query)" + doGet);
            ret.AppendLine("function " + name + ":doPost(query, body)" + doPost);
            ret.AppendLine();

            foreach (Type controller in controllerTypes)
            {
                List<ControllerAction> actions = new(ControllerInspector.Inspect(controller));
                foreach (ControllerAction action in actions)
                    ret.AppendLine(MakeFunctionMeta(action) + "\n");
            }

            ret.AppendLine(@"
function " + name + @":new(sUrl)
  if (sUrl == nil) or (type(sUrl) ~= ""string"") or (sUrl == """") then
    error(""Url expects a string"", 2)
  end
  if (sUrl:sub(-1)~=""/"") then
    sUrl = sUrl..""/""
  end
  t = {}
  setmetatable(t, self)
  self.__index = self
  t.url = sUrl
  return t
end
");
            ret.AppendLine("setmetatable(" + name + ", { __call = function(cls, ...) return cls:new(...) end })");
            ret.AppendLine();
            ret.AppendLine("--Usage:");
            ret.AppendLine("--  myClient = " + name + "(\"http://example.com/app/\")");
            ret.AppendLine("--  result = myClient:Area_Controller_Action(arg1, arg2)");
            return ret.ToString();
        }

#endregion

    }
}
