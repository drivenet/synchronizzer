using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Synchronizzer.Middleware
{
    internal sealed class VersionMiddleware
    {
        private static readonly ReadOnlyMemory<byte> VersionBytes = Encoding.ASCII.GetBytes(Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "?").AsMemory();

        private readonly RequestDelegate _next;

        public VersionMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.HasValue)
            {
                await _next(context);
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
