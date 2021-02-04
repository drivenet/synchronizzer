using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Synchronizzer.Middleware
{
    internal sealed class MetricsReportingMiddleware : IMiddleware
    {
        private static readonly char[] PathChars = new[] { '/' };

        private readonly Components.IMetricsReader _metricsReader;

        public MetricsReportingMiddleware(Components.IMetricsReader metricsReader)
        {
            _metricsReader = metricsReader ?? throw new ArgumentNullException(nameof(metricsReader));
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Method != "GET")
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            var metricName = context.Request.Path.Value.TrimStart(PathChars);
            var metricValue = _metricsReader.GetValue(metricName);
            var metricString = (metricValue ?? 0).ToString(CultureInfo.InvariantCulture);
            var bytes = Encoding.UTF8.GetBytes(metricString);
            var response = context.Response;
            response.ContentLength = bytes.Length;
            await response.Body.WriteAsync(bytes);
        }
    }
}
