using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tmds.Systemd;

namespace Synchronizzer.Composition
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            ConfigureNetworking();
            var commandLineOptions = GetCommandLineOptions(args);
            var appConfiguration = LoadAppConfiguration(commandLineOptions.Config);
            var hostingConfigPath = commandLineOptions.HostingConfig;
            do
            {
                await RunHost(appConfiguration, hostingConfigPath);
            }
            while (ServiceManager.IsRunningAsService);
        }

        private static async Task RunHost(IConfiguration appConfiguration, string hostingConfigPath)
        {
            var hostingOptions = GetHostingOptions(hostingConfigPath);
            using var host = BuildWebHost(hostingOptions, appConfiguration);
            await host.RunAsync();
        }

        private static IConfiguration LoadAppConfiguration(string configPath)
            => new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .AddEnvironmentVariables("GSS_")
                .Build();

        private static CommandLineOptions GetCommandLineOptions(string?[] args)
            => new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build()
                .Get<CommandLineOptions>() ?? new CommandLineOptions();

        private static HostingOptions GetHostingOptions(string configPath)
            => new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false)
                .Build()
                .Get<HostingOptions>() ?? new HostingOptions();

        private static IWebHost BuildWebHost(HostingOptions hostingOptions, IConfiguration appConfiguration)
            => new WebHostBuilder()
                .ConfigureLogging(loggingBuilder => ConfigureLogging(loggingBuilder, hostingOptions))
                .UseSetting(WebHostDefaults.ServerUrlsKey, hostingOptions.Listen)
                .UseKestrel(options => ConfigureKestrel(options))
                .ConfigureServices(services => services.AddSingleton(appConfiguration))
                .UseStartup<Startup>()
                .Build();

        private static void ConfigureLogging(ILoggingBuilder loggingBuilder, HostingOptions hostingOptions)
        {
            loggingBuilder.AddFilter(Filter);
            var hasJournalD = Journal.IsSupported;
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

        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 0;
            options.Limits.MaxRequestHeadersTotalSize = 4096;
            options.Limits.MaxConcurrentConnections = 16;
        }

        private static void ConfigureNetworking()
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DnsRefreshTimeout = 3000;
            ServicePointManager.EnableDnsRoundRobin = true;
            ServicePointManager.ReusePort = true;
        }
    }
}
