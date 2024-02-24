using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace LuaSupport.Mvc
{
    public class LuaOutputFormatter : TextOutputFormatter

    {
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        internal LuaFormatterConfig config = new();

        public LuaOutputFormatter()
        {
            // While there is no official MIME type for Lua, these would be appropriate
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/x-lua"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/x-lua"));
            SupportedEncodings.Add(Encoding.UTF8);
        }

        public LuaOutputFormatter(LuaFormatterConfig config) : this()
        {
            if (!(config == null)) this.config = config;
        }

        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            context.ContentType = "text/plain";
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            HttpResponse response = context.HttpContext.Response;
            LuaSupport.LuaFormatter lf = new(config);
            var stream = response.BodyWriter.AsStream();
            lf.Serialize(stream, context.Object);
            await stream.FlushAsync();
        }

    }
}
