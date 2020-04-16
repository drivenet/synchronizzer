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
            _context = context;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- opening local file is synchronous
        public async Task<Stream?> Read(string objectName, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (Path.DirectorySeparatorChar != '/')
            {
                objectName = objectName.Replace('/', Path.DirectorySeparatorChar);
            }

            var path = _context.FilePath + Path.DirectorySeparatorChar + objectName;
            if (Path.GetFullPath(path) != path)
            {
                throw new ArgumentOutOfRangeException(nameof(path), path, FormattableString.Invariant($"The object name \"{objectName}\" produces an insecure path \"{path}\""));
            }

            try
            {
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
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
