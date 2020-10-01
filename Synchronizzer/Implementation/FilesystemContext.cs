using System;
using System.IO;

namespace Synchronizzer.Implementation
{
    internal sealed class FilesystemContext
    {
        public FilesystemContext(string path)
        {
            if (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar)
            {
                path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            path = path.TrimEnd(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (Path.GetFullPath(path) != path)
            {
                throw new ArgumentOutOfRangeException(nameof(path), path, "Invalid path.");
            }

            FilePath = path;
        }

        public string FilePath { get; }
    }
}
