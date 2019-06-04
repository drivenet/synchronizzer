using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class FilesystemObjectSource : IObjectSource
    {
        private readonly FilesystemContext _context;

        public FilesystemObjectSource(FilesystemContext context)
        {
            _context = context;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- enumerating file system is synchronous
        public async Task<IEnumerable<ObjectInfo>> GetOrdered(string? fromName, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fromName is object)
            {
                return Enumerable.Empty<ObjectInfo>();
            }

            return Iterate("", cancellationToken);
        }

        private IEnumerable<ObjectInfo> Iterate(string path, CancellationToken cancellationToken)
        {
            string? prefix = null;
            foreach (var info in new DirectoryInfo(_context.FilePath + Path.DirectorySeparatorChar + path).GetFileSystemInfos())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ((info.Attributes & (FileAttributes.Encrypted | FileAttributes.Temporary | FileAttributes.Offline)) != 0)
                {
                    continue;
                }

                switch (info)
                {
                    case FileInfo fileInfo:
                        if (prefix == null)
                        {
                            prefix = path;
                            if (Path.DirectorySeparatorChar != '/')
                            {
                                prefix = prefix.Replace(Path.DirectorySeparatorChar, '/');
                            }
                        }

                        yield return new ObjectInfo(prefix + fileInfo.Name, fileInfo.Length);
                        break;

                    case DirectoryInfo directoryInfo:
                        foreach (var item in Iterate(path + directoryInfo.Name + Path.DirectorySeparatorChar, cancellationToken))
                        {
                            yield return item;
                        }

                        break;
                }
            }
        }
    }
}
