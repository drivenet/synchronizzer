using System;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GridFSSyncService.Composition
{
    internal sealed class SyncOptionsConfig : IConfigureOptions<SyncOptions>, IPostConfigureOptions<SyncOptions>
    {
        private static readonly Regex UriVars = new Regex(@"\$(\w+)\$", RegexOptions.CultureInvariant);

        private readonly IConfiguration _configuration;

        public SyncOptionsConfig(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(SyncOptions options)
        {
            _configuration.Bind(options);
        }

        public void PostConfigure(string name, SyncOptions options)
        {
            var jobs = options.Jobs;
            if (jobs == null)
            {
                return;
            }

            var template = _configuration.GetSection("vars");
            foreach (var job in jobs)
            {
                job.Local = ReplaceUri(job.Local, template);
                job.Remote = ReplaceUri(job.Remote, template);
                job.Recycle = ReplaceUri(job.Recycle, template);
            }
        }

        private static Uri? ReplaceUri(Uri? uri, IConfiguration template)
        {
            if (uri is object && uri.IsAbsoluteUri)
            {
                var query = UriVars.Replace(
                    uri.Query,
                    match => template.GetValue(match.Groups[1].Value, ""));
                if (query != uri.Query)
                {
                    uri = new UriBuilder(uri) { Query = query }.Uri;
                }
            }

            return uri;
        }
    }
}
