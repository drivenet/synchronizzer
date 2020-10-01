using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class FilesystemObjectWriter : IObjectWriter
    {
        private readonly FilesystemContext _context;
        private readonly FilesystemContext? _recycleContext;

        public FilesystemObjectWriter(FilesystemContext context, FilesystemContext? recycleContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _recycleContext = recycleContext;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- deleting local file is synchronous
        public async Task Delete(string objectName, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var path = FilesystemUtils.PreparePath(objectName, _context);
            var recyclePath = _recycleContext is object ? FilesystemUtils.PreparePath(objectName, _recycleContext) : null;
            cancellationToken.ThrowIfCancellationRequested();
            if (recyclePath is object)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(recyclePath));
                try
                {
                    File.Move(path, recyclePath, true);
                }
                catch (FileNotFoundException)
                {
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }
            }
            else
            {
                try
                {
                    File.Delete(path);
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }
            }

            var root = Path.GetDirectoryName(FilesystemUtils.PreparePath("", _context));
            if (root is null)
            {
                return;
            }

            var rootLength = root.Length;
            while (true)
            {
                var directory = Path.GetDirectoryName(path);
                if (directory is null)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    break;
                }

                var dirLength = directory.Length;
                if (dirLength == rootLength)
                {
                    break;
                }

                if (dirLength < rootLength)
                {
                    throw new InvalidProgramException(FormattableString.Invariant($"Recursive deletion of directory \"{directory}\" attempted to escape root \"{root}\"."));
                }

                Directory.Delete(directory);
                path = directory;
            }
        }

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            var path = FilesystemUtils.PreparePath(objectName, _context);
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete);
            await readOnlyInput.CopyToAsync(file, cancellationToken);
        }
    }
}
