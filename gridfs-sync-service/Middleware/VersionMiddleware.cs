using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSSyncService.Middleware
{
    internal sealed class VersionMiddleware
    {
        private static readonly string VersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);

        private readonly RequestDelegate _next;

        public VersionMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.Request.Path.HasValue)
            {
                if (context.Request.Method != "GET")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return Task.CompletedTask;
                }

                return context.Response.WriteAsync(VersionString);
            }

            return _next(context);
        }
    }
}
