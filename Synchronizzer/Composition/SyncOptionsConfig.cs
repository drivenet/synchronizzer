﻿using System;
using System.Net;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Synchronizzer.Composition
{
    internal sealed partial class SyncOptionsConfig : IPostConfigureOptions<SyncOptions>
    {
        private static readonly Regex UriVars = CreateUriVars();

        private readonly IConfiguration _configuration;

        public SyncOptionsConfig(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void PostConfigure(string? name, SyncOptions options)
        {
            var jobs = options.Jobs;
            if (jobs is null)
            {
                return;
            }

            var template = _configuration.GetSection("vars");
            foreach (var job in jobs)
            {
                job.Origin = ReplaceAddress(job.Origin, template);
                job.Destination = ReplaceAddress(job.Destination, template);
                job.Recycle = ReplaceAddress(job.Recycle, template);
            }
        }

        private static string? ReplaceAddress(string? address, IConfiguration template)
        {
            if (address is null)
            {
                return null;
            }

            return UriVars.Replace(
                address,
                match =>
                {
                    var key = match.Groups[1].Value;
                    var value = template.GetValue(key, key);
                    if (value != key)
                    {
                        value = value is not null ? WebUtility.UrlEncode(value) : key;
                    }

                    return value;
                });
        }

        [GeneratedRegex(@"\$(\w+)\$", RegexOptions.CultureInvariant)]
        private static partial Regex CreateUriVars();
    }
}
