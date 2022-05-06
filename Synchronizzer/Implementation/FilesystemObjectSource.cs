using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.FormattableString;

namespace Synchronizzer.Implementation
{
    internal sealed class FilesystemObjectSource : IObjectSource
    {
        private static readonly FileAttributes HiddenMask = Environment.OSVersion.Platform == PlatformID.Win32NT ? FileAttributes.Hidden : default;

        private readonly FilesystemContext _context;

        public FilesystemObjectSource(FilesystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- enumerating file system is synchronous
        public async IAsyncEnumerable<IReadOnlyCollection<ObjectInfo>> GetOrdered([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var result = Enumerate(null, cancellationToken).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            result.Sort();
            yield return result;
        }

        private IEnumerable<ObjectInfo> Enumerate(string? fromName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                        var isHidden = (fileInfo.Attributes & HiddenMask) != 0
                            || name.StartsWith(FilesystemConstants.LockPath, StringComparison.OrdinalIgnoreCase);
                        yield return new(name, fileInfo.Length, isHidden, fileInfo.LastWriteTimeUtc);
                    }
                }
            }
        }
    }
}
