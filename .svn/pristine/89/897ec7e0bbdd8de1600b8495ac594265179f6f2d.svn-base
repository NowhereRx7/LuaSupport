using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LuaSupport.Mvc
{
    public class LuaInputFormatter : TextInputFormatter
    {

        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        internal LuaFormatterConfig config = new();

        public LuaInputFormatter()
        {
            // While there is no official MIME type for Lua, these would be appropriate
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/x-lua"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/x-lua"));
            SupportedEncodings.Add(Encoding.UTF8);
        }
        public LuaInputFormatter(LuaFormatterConfig config) : this()
        {
            if (!(config == null)) this.config = config;
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            //TODO: Computercraft versions older than 1.7 can't set the headers to match the above filters, it will be application/x-www-form-urlencoded
            //context.HttpContext.Request.ContentType would be what to check; then unencode twice
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));
            using (var streamReader = context.ReaderFactory(context.HttpContext.Request.Body, encoding))
            {
                var content = await streamReader.ReadToEndAsync();
                if (String.IsNullOrEmpty(content))
                    return InputFormatterResult.NoValue();
                LuaSupport.LuaFormatter formatter = new(config);
                using (var ms = new System.IO.MemoryStream())
                {
                    ms.Write(Encoding.UTF8.GetBytes(content));
                    if (context.ModelType != null)
                    {
                        object o = formatter.Deserialize(ms, context.ModelType);
                        return await InputFormatterResult.SuccessAsync(o);
                    }
                    else
                    {
                        object o = formatter.Deserialize(ms);
                        return await InputFormatterResult.SuccessAsync(o);
                    }
                }
            }
        }
    }
}
