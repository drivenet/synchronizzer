using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace Synchronizzer.Composition
{
    internal sealed class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SyncOptions>(_configuration);
            services.ConfigureOptions<SyncOptionsConfig>();
#pragma warning disable RS0030 // Do not use banned APIs -- required for DI
            services.AddSingleton(TimeProvider.System);
#pragma warning restore RS0030 // Do not use banned APIs
            services.AddSingleton<Components.MetricsContainer>();
            services.AddSingleton<Components.IMetricsReader>(provider => provider.GetRequiredService<Components.MetricsContainer>());
            services.AddSingleton<Components.IMetricsWriter>(provider => provider.GetRequiredService<Components.MetricsContainer>());
            services.AddHostedService<Implementation.SyncService>();
            services.AddSingleton<ISyncTimeHolderResolver>(SyncTimeHolderResolver.Instance);
            services.AddSingleton<JobManagingSynchronizer>();
            services.AddSingleton<Implementation.ISynchronizer>(
                provider => new TracingRootSynchronizer(
                    provider.GetRequiredService<JobManagingSynchronizer>(),
                    provider.GetRequiredService<ILogger<TracingRootSynchronizer>>()));
            services.AddSingleton<SynchronizationJobFactory>();
            services.AddSingleton<ISynchronizationJobFactory>(
                provider => new RobustJobFactory(
                    new TracingJobFactory(
                        provider.GetRequiredService<SynchronizationJobFactory>(),
                        provider.GetRequiredService<ILogger<TracingJobFactory>>())));
            services.AddSingleton<IEnumerable<SyncInfo>, SyncInfoResolver>();
            services.AddSingleton<ISynchronizerFactory, SynchronizerFactory>();
            services.AddSingleton<IOriginReaderResolver, OriginReaderResolver>();
            services.AddSingleton<IDestinationWriterResolver, DestinationWriterResolver>();
            services.AddSingleton<IQueuingTaskManagerSelector, QueuingTaskManagerSelector>();
            services.AddSingleton<Implementation.IQueuingSettings, QueuingSettings>();

            services.AddSingleton<Middleware.MetricsReportingMiddleware>();
            services.AddSingleton<Middleware.VersionMiddleware>();

            services.AddSingleton<RecyclableMemoryStreamManager>();
        }

#pragma warning disable CA1822 // Mark members as static -- future-proofing
        public void Configure(IApplicationBuilder app)
#pragma warning restore CA1822 // Mark members as static
        {
            app.Map("/metrics", builder => builder.UseMiddleware<Middleware.MetricsReportingMiddleware>());
            app.Map("/version", builder => builder.UseMiddleware<Middleware.VersionMiddleware>());
        }
    }
}
