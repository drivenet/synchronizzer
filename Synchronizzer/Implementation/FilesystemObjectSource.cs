﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.FormattableString;

namespace Synchronizzer.Implementation
{
    internal sealed class FilesystemObjectSource : IObjectSource
    {
        private readonly FilesystemContext _context;

        public FilesystemObjectSource(FilesystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- enumerating file system is synchronous
        public async Task<ObjectsBatch> GetOrdered(string? continuationToken, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (continuationToken is { Length: 0 })
            {
                return ObjectsBatch.Empty;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var result = Enumerate(continuationToken, cancellationToken).ToList();
            result.Sort();
            return new(result, "");
        }

        private IEnumerable<ObjectInfo> Enumerate(string? fromName, CancellationToken cancellationToken)
        {
            var directoryInfo = new DirectoryInfo(_context.FilePath);
            var prefix = directoryInfo.FullName + Path.DirectorySeparatorChar;
            foreach (var info in directoryInfo.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ((info.Attributes & (FileAttributes.Encrypted | FileAttributes.Temporary | FileAttributes.Offline)) != 0)
                {
                    continue;
                }

                if (info is FileInfo fileInfo)
                {
                    var name = fileInfo.FullName;
                    if (!name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(Invariant($"The name \"{name}\" does not start with expected prefix \"{prefix}\"."));
                    }

                    name = name.Substring(prefix.Length);
                    if (Path.DirectorySeparatorChar != '/')
                    {
                        name = name.Replace(Path.DirectorySeparatorChar, '/');
                    }

                    if (string.CompareOrdinal(name, fromName) > 0)
                    {
                        var isHidden = (fileInfo.Attributes & FileAttributes.Hidden) != 0
                            || name.StartsWith(FilesystemConstants.LockPath, StringComparison.OrdinalIgnoreCase);
                        yield return new ObjectInfo(name, fileInfo.Length, isHidden);
                    }
                }
            }
        }
    }
}
