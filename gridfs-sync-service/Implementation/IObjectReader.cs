using System.IO;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal interface IObjectReader
    {
        Task<Stream> Read(string name);
    }
}
