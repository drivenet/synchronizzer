using System;

namespace GridFSSyncService.Implementation
{
    internal static class FilesystemUtils
    {
        public static FilesystemContext CreateContext(Uri uri)
        {
            if (!uri.IsAbsoluteUri
                || !uri.IsFile
                || uri.IsUnc)
            {
                throw new ArgumentOutOfRangeException(nameof(uri), "Invalid filesystem URI.");
            }

            return new FilesystemContext(uri.LocalPath);
        }
    }
}
