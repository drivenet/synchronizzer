using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class FilesystemObjectReader : IObjectReader
    {
        private readonly FilesystemContext _context;

        public FilesystemObjectReader(FilesystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- opening local file is synchronous
        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var path = FilesystemUtils.PreparePath(objectName, _context);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
                return new ReadObject(file, file.Length);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
