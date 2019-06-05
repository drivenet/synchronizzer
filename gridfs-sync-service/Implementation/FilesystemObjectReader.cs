using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class FilesystemObjectReader : IObjectReader
    {
        private readonly FilesystemContext _context;

        public FilesystemObjectReader(FilesystemContext context)
        {
            _context = context;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- opening local file is synchronous
        public async Task<Stream> Read(string objectName, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (Path.DirectorySeparatorChar != '/' && Path.AltDirectorySeparatorChar != '/')
            {
                objectName = objectName.Replace('/', Path.DirectorySeparatorChar);
            }

            Stream stream = File.Open(_context.FilePath + Path.DirectorySeparatorChar + objectName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            return stream;
        }
    }
}
