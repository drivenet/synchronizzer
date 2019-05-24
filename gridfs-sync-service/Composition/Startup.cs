using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridFSSyncService.Composition
{
    internal sealed class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<Components.IMetricsReader, Components.NullMetricsReader>();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.Map("/metrics", ConfigureMetrics);
            app.Map("/version", ConfigureVersion);
        }

        private static void ConfigureMetrics(IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<Middleware.MetricsReportingMiddleware>();
        }

        private static void ConfigureVersion(IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<Middleware.VersionMiddleware>();
        }
    }
}
