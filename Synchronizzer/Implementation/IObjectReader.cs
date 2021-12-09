using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IObjectReader
    {
        Task<ReadObject?> Read(string objectName, CancellationToken cancellationToken);
    }
}
