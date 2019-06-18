using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Synchronizzer.Middleware
{
    internal sealed class VersionMiddleware
    {
        private static readonly string VersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);

        private readonly RequestDelegate _next;

        public VersionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.HasValue)
            {
                if (context.Request.Method != "GET")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return;
                }

                await context.Response.WriteAsync(VersionString);
                return;
            }

            await _next(context);
        }
    }
}
