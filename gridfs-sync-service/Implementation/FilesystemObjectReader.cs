using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class FilesystemObjectReader : IObjectReader
    {
        public Task<Stream> Read(string name, CancellationToken cancellationToken)
        {
            Stream stream = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            return Task.FromResult(stream);
        }
    }
}
