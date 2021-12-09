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
            var recyclePath = _recycleContext is not null ? FilesystemUtils.PreparePath(objectName, _recycleContext) : null;
            cancellationToken.ThrowIfCancellationRequested();
            if (recyclePath is not null)
            {
                if (Path.GetDirectoryName(path) is { } directory)
                {
                    Directory.CreateDirectory(directory);
                }

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
                if (Path.GetDirectoryName(path) is not { } directory)
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

        public async Task Upload(string objectName, ReadObject readObject, CancellationToken cancellationToken)
        {
            if (readObject is null)
            {
                throw new ArgumentNullException(nameof(readObject));
            }

            var path = FilesystemUtils.PreparePath(objectName, _context);
            cancellationToken.ThrowIfCancellationRequested();
            if (Path.GetDirectoryName(path) is { } directory)
            {
                Directory.CreateDirectory(directory);
            }

            using var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete);
            await readObject.Stream.CopyToAsync(file, cancellationToken);
        }
    }
}
