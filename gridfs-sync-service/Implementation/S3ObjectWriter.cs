using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3.Model;

namespace GridFSSyncService.Implementation
{
    internal sealed class S3ObjectWriter : IObjectWriter
    {
        private readonly S3WriteContext _context;

        public S3ObjectWriter(S3WriteContext context)
        {
            _context = context;
        }

        public Task Delete(string objectName, CancellationToken cancellationToken)
            => _context.S3.DeleteAsync(_context.BucketName, objectName, null, cancellationToken);

        public Task Flush(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task Upload(string objectName, Stream readOnlyInput, CancellationToken cancellationToken)
        {
            var request = new PutObjectRequest
            {
                BucketName = _context.BucketName,
                Key = objectName,
                InputStream = readOnlyInput,
                StorageClass = _context.StorageClass,
            };
            await _context.S3.PutObjectAsync(request, cancellationToken);
        }
    }
}
