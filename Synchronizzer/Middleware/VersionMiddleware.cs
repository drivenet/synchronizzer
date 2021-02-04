using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Synchronizzer.Middleware
{
    internal sealed class VersionMiddleware : IMiddleware
    {
        private static readonly ReadOnlyMemory<byte> VersionBytes = Encoding.ASCII.GetBytes(Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "?").AsMemory();

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path.HasValue)
            {
                await next(context);
                return;
            }

            if (context.Request.Method != "GET")
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            var response = context.Response;
            response.ContentLength = VersionBytes.Length;
            await response.Body.WriteAsync(VersionBytes);
        }
    }
}
