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

        public async Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken)
        {
            var path = FilesystemUtils.PreparePath(objectName, _context);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, sizeof(long), true);
                try
                {
                    return new ReadObject(file, file.Length);
                }
                catch
                {
                    await file.DisposeAsync();
                    throw;
                }
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
