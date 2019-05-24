using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSSyncService.Middleware
{
    internal sealed class MetricsReportingMiddleware
    {
        private static readonly char[] PathChars = new[] { '/' };

        private readonly RequestDelegate _next;

        private readonly Components.IMetricsReader _metricsReader;

        public MetricsReportingMiddleware(RequestDelegate next, Components.IMetricsReader metricsReader)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _metricsReader = metricsReader ?? throw new ArgumentNullException(nameof(metricsReader));
        }

        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var metricName = context.Request.Path.Value.TrimStart(PathChars);
            var metricValue = _metricsReader.GetValue(metricName);
            if (metricValue != null)
            {
                if (context.Request.Method != "GET")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return Task.CompletedTask;
                }

                var metricString = ((double)metricValue).ToString(CultureInfo.InvariantCulture);
                return context.Response.WriteAsync(metricString);
            }

            return _next(context);
        }
    }
}
