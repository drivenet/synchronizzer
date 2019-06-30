using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizzer.Implementation
{
    internal interface IObjectWriter
    {
        Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken);

        Task Delete(string objectName, CancellationToken cancellationToken);
    }
}
