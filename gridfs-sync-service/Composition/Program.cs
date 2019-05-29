using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GridFSSyncService.Composition
{
    public static class Program
    {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly -- StyleCop fails to handle nullable arrays
        public static void Main(string?[]? args)
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
        {
            var commandLineOptions = GetCommandLineOptions(args);
            var appConfiguration = LoadAppConfiguration(commandLineOptions.Config);
            while (true)
            {
                var hostingOptions = GetHostingOptions(commandLineOptions.HostingConfig);
                using (var host = BuildWebHost(hostingOptions, appConfiguration))
                {
                    host.Run();
                }
            }
        }

        private static IConfiguration LoadAppConfiguration(string configPath)
            => new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .Build();

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

        private static IWebHost BuildWebHost(HostingOptions hostingOptions, IConfiguration appConfiguration)
            => new WebHostBuilder()
                .UseSetting(WebHostDefaults.ServerUrlsKey, hostingOptions.Listen)
                .ConfigureLogging(loggingBuilder => ConfigureLogging(loggingBuilder, hostingOptions))
                .UseKestrel(options => ConfigureKestrel(options))
                .ConfigureServices(services => services.AddSingleton(appConfiguration))
                .UseStartup<Startup>()
                .Build();

        private static void ConfigureLogging(ILoggingBuilder loggingBuilder, HostingOptions hostingOptions)
        {
            loggingBuilder.AddFilter((category, level) => level >= LogLevel.Warning || level == LogLevel.Trace);
            var hasJournalD = Tmds.Systemd.Journal.IsSupported;
            if (hasJournalD)
            {
                loggingBuilder.AddJournal(options => options.SyslogIdentifier = "gridfs-sync-service");
            }

            if (!hasJournalD || hostingOptions.ForceConsoleLogging)
            {
                loggingBuilder.AddConsole(options => options.DisableColors = true);
            }
        }

        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 0;
            options.Limits.MaxRequestHeadersTotalSize = 4096;
            options.Limits.MaxConcurrentConnections = 10;
        }
    }
}
