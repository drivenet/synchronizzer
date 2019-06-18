using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Synchronizzer.Composition
{
    public static class Program
    {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly -- StyleCop fails to handle nullable arrays
        public static async Task Main(string?[]? args)
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
        {
            ConfigureNetworking();
            var commandLineOptions = GetCommandLineOptions(args);
            while (true)
            {
                var hostingOptions = GetHostingOptions(commandLineOptions.HostingConfig);
                using (var host = BuildWebHost(hostingOptions, commandLineOptions.Config))
                {
                    await host.RunAsync();
                }
            }
        }

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly -- StyleCop fails to handle nullable arrays
        private static CommandLineOptions GetCommandLineOptions(string?[]? args)
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
            => new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build()
                .Get<CommandLineOptions>() ?? new CommandLineOptions();

        private static HostingOptions GetHostingOptions(string configPath)
            => new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false)
                .Build()
                .Get<HostingOptions>() ?? new HostingOptions();

        private static IWebHost BuildWebHost(HostingOptions hostingOptions, string configPath)
            => WebHostBuilderExtensions.UseStartup<Startup>(new WebHostBuilder()
                .ConfigureLogging(loggingBuilder => ConfigureLogging(loggingBuilder, hostingOptions))
                .ConfigureAppConfiguration(configurationBuilder => ConfigureConfiguration(configurationBuilder, configPath)))
                .UseSetting(WebHostDefaults.ServerUrlsKey, hostingOptions.Listen)
                .UseKestrel(options => ConfigureKestrel(options))
                .UseStartup<Startup>()
                .Build();

        private static void ConfigureLogging(ILoggingBuilder loggingBuilder, HostingOptions hostingOptions)
        {
            loggingBuilder.AddFilter(Filter);
            var hasJournalD = Tmds.Systemd.Journal.IsSupported;
            if (hasJournalD)
            {
                loggingBuilder.AddJournal(options => options.SyslogIdentifier = "synchronizzer");
            }

            if (!hasJournalD || hostingOptions.ForceConsoleLogging)
            {
                loggingBuilder.AddConsole(options =>
                {
                    options.DisableColors = true;
                    options.IncludeScopes = true;
                });
            }

            bool Filter(string category, LogLevel level)
                => level >= LogLevel.Warning
                    || ((level >= LogLevel.Information || hostingOptions.DebugLogging)
                        && !category.StartsWith("Microsoft.AspNetCore.", StringComparison.OrdinalIgnoreCase));
        }

        private static void ConfigureConfiguration(IConfigurationBuilder configurationBuilder, string configPath)
        {
            configurationBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
            configurationBuilder.AddEnvironmentVariables("GSS_");
        }

        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 0;
            options.Limits.MaxRequestHeadersTotalSize = 4096;
            options.Limits.MaxConcurrentConnections = 10;
        }

        private static void ConfigureNetworking()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DnsRefreshTimeout = 3000;
            ServicePointManager.EnableDnsRoundRobin = true;
            ServicePointManager.ReusePort = true;
        }
    }
}
