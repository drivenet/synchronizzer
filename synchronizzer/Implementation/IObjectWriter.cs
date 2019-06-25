using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IObjectWriter
    {
        Task Lock(CancellationToken cancellationToken);

        Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken);

        Task Delete(string objectName, CancellationToken cancellationToken);

        Task Flush(CancellationToken cancellationToken);
    }
}
