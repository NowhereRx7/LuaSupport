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
    public class ComputercraftClientCreator2
    {

        private const string replaceRouteTokens = @"
      local args = arg or table.pack(...)
      local route = action.route
      for i,v in ipairs(action.parameters) do
        route = string.gsub(route, ""{""..v..""(:[^}])*}"", textutils.urlEncode(args[i]))
      end
      return route
    end";

        private const string doStart = @"
      if (self.actions[action] == nil) then
        error(""Action not found:"" .. action, 2)
      end
      local act = self.actions[action]
      local args = arg or table.pack(...)
      if (args.n < #act.parameters) then
        error(""Not enough parameters supplied for action."", 2)
      end
      local query = self.url .. self.replaceRouteTokens(act, ...)";

        private const string doGet = doStart + @"
      local queryString = """"
      for k,v in pairs(act.queryParameters) do
        if (args[v] ~= nil) then
          queryString = queryString .. ""&"" .. k .. ""="" .. textutils.urlEncode(args[v])
        end
      end
      if (queryString ~= """") then
        queryString = ""?"" .. queryString:sub(2)
        query = query .. queryString
      end
      local headers = {['Accept'] = ""application/x-lua, text/x-lua, text/plain'"", ['content-type'] = ""application/x-lua""}
      local h = http.get(query, headers)" + doEnd;

        private const string doPost = doStart + @"
      local body = """"
      for k,v in pairs(act.queryParameters) do
        body = textutils.serialize(args[v])
      end
      local headers = {['Accept'] = ""application/x-lua, text/x-lua, text/plain'"", ['content-type'] = ""application/x-lua""}
      local h = http.post(query, body, headers)" + doEnd;

        private const string doEnd = @"
      if (h == nil) then 
        error(""Failed to get a valid response during HTTP call."") -- , 2)
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

        //TODO: Fix indenting.  Probably a single tab char at the start that can be replaced by appropriate spaces.

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

        private static string ActionName(ControllerAction action)
        {
            StringBuilder ret = new();
            if (!String.IsNullOrEmpty(action.Area))
                ret.Append(action.Area + "_");
            ret.Append(action.Controller + "_");
            ret.Append(action.Name);
            return ret.ToString();
        }

        private static string MakeActionData(ControllerAction action)
        {
            StringBuilder ret = new();
            ret.AppendLine("      ['" + ActionName(action) + "'] = {");
            ret.AppendLine("        route = \"" + action.Route + "\",");
            ret.Append("        parameters = {");
            if (action.Parameters.Count == 0)
                ret.AppendLine(" },");
            else
            {
                ret.AppendLine();
                foreach (ParameterInfo paramaterInfo in action.Parameters)
                    ret.AppendLine("          \"" + paramaterInfo.Name + "\",");
                ret.AppendLine("        },");
            }
            ret.Append("        queryParameters = {");
            if (action.QueryParameters.Count == 0)
                ret.AppendLine(" },");
            else
            {
                ret.AppendLine();
                for (var pi = 0; pi < action.Parameters.Count; pi++)
                {
                    for (var qi = 0; qi < action.QueryParameters.Count; qi++)
                    {
                        if (action.Parameters[pi].Name == action.QueryParameters[qi])
                        {
                            ret.AppendLine("          ['" + action.Parameters[pi].Name + "'] = " + (pi + 1).ToString() + ",");
                        }
                    }
                }
                ret.AppendLine("        },");
            }
            ret.Append("      },");
            return ret.ToString();
        }

        private static string FunctionSigClosure(ControllerAction action)
        {
            StringBuilder ret = new();
            //UNDONE: How to best do commenting with closures
            ret.Append("  local ");
            ret.Append(ActionName(action));
            ret.Append(" = function(");
            List<string> args = new();
            foreach (ParameterInfo pi in action.Parameters)
                args.Add(pi.Name);
            ret.Append(string.Join(", ", args.ToArray()));
            ret.Append(')');
            return ret.ToString();
        }

        private static string MakeFunction(ControllerAction action)
        {
            StringBuilder ret = new();
            ret.Append(Comments(action));
            ret.AppendLine(FunctionSigClosure(action));
            List<string> list = new();
            foreach (ParameterInfo parameterInfo in action.Parameters)
                list.Add(parameterInfo.Name);
            if (action.Verb == System.Net.Http.HttpMethod.Get)
                ret.Append("    return self:doGet");
            if (action.Verb == System.Net.Http.HttpMethod.Post)
                ret.Append("    return self:doPost");
            ret.Append("(\"" + ActionName(action) + "\"" + (list.Count == 0 ? string.Empty : ", "));
            ret.Append(String.Join(", ", list));
            ret.AppendLine(")");

            ret.Append("  end");
            return ret.ToString();
        }

        //http://www.lua.org/pil/16.4.html  Privacy (Closure classes)


        public static string Create(string name, Type[] controllerTypes)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (!Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9_]*$"))
                throw new ArgumentException("Name must follow standard naming conventions for Lua variables.", nameof(name));

            StringBuilder ret = new();
            ret.AppendLine("function " + name + "(url)");
            ret.AppendLine(@"  if (url == nil) or (type(url) ~= ""string"") or (url == """") then
    error(""Url expects a string"", 2)
  end
  if (url:sub(-1)~=""/"") then
    url = url..""/""
  end");
            ret.AppendLine("  local self = {");
            ret.AppendLine("    url = \"\",");
            ret.AppendLine("    replaceRouteTokens = function(action, ...)" + replaceRouteTokens + ",");
            ret.AppendLine("    doGet = function(self, action, ...)" + doGet + ",");
            ret.AppendLine("    doPost = function(self, action, ...)" + doPost + ",");
            ret.AppendLine("    actions = {");
            foreach (Type controller in controllerTypes)
            {
                List<ControllerAction> actions = new(ControllerInspector.Inspect(controller));
                foreach (ControllerAction action in actions)
                    ret.AppendLine(MakeActionData(action));
            }
            ret.AppendLine("    },");
            ret.AppendLine("  }");
            foreach (Type controller in controllerTypes)
            {
                List<ControllerAction> actions = new(ControllerInspector.Inspect(controller));
                foreach (ControllerAction action in actions)
                    ret.AppendLine(MakeFunction(action) + "\n");
                //TODO: New calls, way shorter
                // function(...) self:doGet(actionName, ...)
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
    }
}
