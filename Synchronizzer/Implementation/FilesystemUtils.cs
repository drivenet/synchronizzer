using System;
using System.IO;

namespace Synchronizzer.Implementation
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

        public static string PreparePath(string objectName, FilesystemContext context)
        {
            if (objectName is null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (Path.DirectorySeparatorChar != '/')
            {
                objectName = objectName.Replace('/', Path.DirectorySeparatorChar);
            }

            var path = context.FilePath + Path.DirectorySeparatorChar + objectName;
            if (Path.GetFullPath(path) != path)
            {
                throw new ArgumentOutOfRangeException(nameof(path), path, FormattableString.Invariant($"The object name \"{objectName}\" produces an insecure path \"{path}\""));
            }

            return path;
        }
    }
}
