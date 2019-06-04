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
        public async Task<Stream> Read(string name, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (Path.DirectorySeparatorChar != '/' && Path.AltDirectorySeparatorChar != '/')
            {
                name = name.Replace('/', Path.DirectorySeparatorChar);
            }

            Stream stream = File.Open(_context.FilePath + Path.DirectorySeparatorChar + name, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            return stream;
        }
    }
}
