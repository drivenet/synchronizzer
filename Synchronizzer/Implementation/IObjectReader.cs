using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IObjectReader
    {
        Task<Stream?> Read(string objectName, CancellationToken cancellationToken);
    }
}
