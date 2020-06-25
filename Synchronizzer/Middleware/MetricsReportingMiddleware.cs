using System.Globalization;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Synchronizzer.Middleware
{
    internal sealed class MetricsReportingMiddleware
    {
        private static readonly char[] PathChars = new[] { '/' };

        private readonly Components.IMetricsReader _metricsReader;

#pragma warning disable CA1801 // Remove unused parameter -- required for middleware
        public MetricsReportingMiddleware(RequestDelegate next, Components.IMetricsReader metricsReader)
#pragma warning restore CA1801 // Remove unused parameter
        {
            _metricsReader = metricsReader;
        }

        public async Task Invoke(HttpContext context)
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
            await response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
