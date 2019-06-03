using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            services.AddSingleton<Components.IMetricsReader>(Components.NullMetricsReader.Instance);
            services.AddSingleton<Components.ITimeSource>(Components.SystemTimeSource.Instance);
            services.AddHostedService<Implementation.SyncService>();
            services.AddSingleton<Implementation.FilesystemObjectSource>();
            services.AddSingleton<Implementation.FilesystemObjectReader>();
            services.AddSingleton<Implementation.S3ObjectSource>();
            services.AddSingleton<Implementation.S3ObjectWriter>();
            var s3Context = Implementation.S3Utils.CreateContext(_configuration.GetSection("s3"));
            services.AddSingleton(s3Context);
            services.AddSingleton<Implementation.S3Context>(s3Context);
            services.AddSingleton<Implementation.ISynchronizer>(provider =>
                new Implementation.RobustSynchronizer(
                    new Implementation.Synchronizer(
                        provider.GetRequiredService<Implementation.FilesystemObjectSource>(),
                        provider.GetRequiredService<Implementation.FilesystemObjectReader>(),
                        provider.GetRequiredService<Implementation.S3ObjectSource>(),
                        provider.GetRequiredService<Implementation.S3ObjectWriter>()),
                    provider.GetRequiredService<ILogger<Implementation.ISynchronizer>>()));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Map("/metrics", builder => builder.UseMiddleware<Middleware.MetricsReportingMiddleware>());
            app.Map("/version", builder => builder.UseMiddleware<Middleware.VersionMiddleware>());
        }
    }
}
