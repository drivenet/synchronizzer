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
                throw new ArgumentOutOfRangeException(
                    nameof(uri),
                    uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped),
                    "Invalid filesystem URI.");
            }

            return new FilesystemContext(uri.LocalPath);
        }
    }
}
