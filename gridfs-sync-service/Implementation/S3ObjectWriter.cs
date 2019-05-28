using System;
using System.IO;
using System.Threading.Tasks;

namespace GridFSSyncService.Implementation
{
    internal sealed class S3ObjectWriter : IObjectWriter
    {
        public Task Delete(string objectName)
        {
            throw new NotImplementedException();
        }

        public Task Flush() => Task.CompletedTask;

        public Task Upload(string objectName, Stream readOnlyInput)
        {
            throw new NotImplementedException();
        }
    }
}
