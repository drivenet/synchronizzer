using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal sealed class FilesystemObjectWriterLocker : IObjectWriterLocker
    {
        private const string LockExtension = ".lock";

        private static readonly Regex LockNameFilter = new("^[a-zA-Z0-9_-]{1,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly FilesystemContext _context;
        private readonly string _lockName;
        private bool _isLocked;

        public FilesystemObjectWriterLocker(FilesystemContext context, string lockName)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (!LockNameFilter.IsMatch(lockName))
            {
                throw new ArgumentException("Invalid filesystem lock name.", nameof(lockName));
            }

            _lockName = lockName;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- deleting file is synchronous
        public async Task Clear(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _isLocked = false;
            var name = FilesystemUtils.PreparePath(FilesystemConstants.LockPath + _lockName + LockExtension, _context);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Delete(name);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously -- creating file is synchronous
        public async Task Lock(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var now = DateTime.UtcNow;
            var lockLifetime = TimeSpan.FromMinutes(3);
            var threshold = now - lockLifetime;
            var path = FilesystemUtils.PreparePath(FilesystemConstants.LockPath, _context);
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(path);
            foreach (var key in Directory.GetFiles(path, "*" + LockExtension))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lockTime = File.GetLastWriteTime(key).ToUniversalTime();
                if (lockTime < threshold)
                {
                    File.Delete(key);
                }
                else
                {
                    var lockName = Path.GetFileNameWithoutExtension(key);
                    if (lockName == _lockName)
                    {
                        break;
                    }

                    var message = _isLocked
                        ? FormattableString.Invariant($"The filesystem lock \"{_lockName}\" was overriden by \"{key}\" (time: {lockTime:o}, threshold: {threshold:o}).")
                        : FormattableString.Invariant($"The filesystem lock \"{_lockName}\" is prevented by \"{key}\" (time: {lockTime:o}, threshold: {threshold:o}).");

                    _isLocked = false;
                    throw new OperationCanceledException(message);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Create(path + _lockName + LockExtension).Dispose();
            _isLocked = true;
        }

        public override string ToString() => FormattableString.Invariant($"FilesystemObjectWriterLocker(\"{_lockName}\")");
    }
}
