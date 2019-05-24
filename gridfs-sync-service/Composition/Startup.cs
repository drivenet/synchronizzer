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
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Components.IMetricsReader, Components.NullMetricsReader>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Map("/metrics", builder => builder.UseMiddleware<Middleware.MetricsReportingMiddleware>());
            app.Map("/version", builder => builder.UseMiddleware<Middleware.VersionMiddleware>());
        }
    }
}
