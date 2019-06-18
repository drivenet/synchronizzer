using System.Net;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Synchronizzer.Composition
{
    internal sealed class SyncOptionsConfig : IPostConfigureOptions<SyncOptions>
    {
        private static readonly Regex UriVars = new Regex(@"\$(\w+)\$", RegexOptions.CultureInvariant);

        private readonly IConfiguration _configuration;

        public SyncOptionsConfig(IConfiguration configuration)
        {
            _configuration = configuration;
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
                job.Local = ReplaceAddress(job.Local, template);
                job.Remote = ReplaceAddress(job.Remote, template);
                job.Recycle = ReplaceAddress(job.Recycle, template);
            }
        }

        private static string? ReplaceAddress(string? address, IConfiguration template)
        {
            if (address is object)
            {
                address = UriVars.Replace(
                    address,
                    match =>
                    {
                        var key = match.Groups[1].Value;
                        var value = template.GetValue(key, key);
                        if (value != key)
                        {
                            value = WebUtility.UrlEncode(value);
                        }

                        return value;
                    });
            }

            return address;
        }
    }
}
