using System.IO;
using System.Threading.Tasks;

using Amazon.S3;

namespace GridFSSyncService.Implementation
{
    internal sealed class S3ObjectWriter : IObjectWriter
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;

        public S3ObjectWriter(IAmazonS3 s3, string bucketName)
        {
            _s3 = s3;
            _bucketName = bucketName;
        }

        public Task Delete(string objectName)
            => _s3.DeleteAsync(_bucketName, objectName, null);

        public Task Flush() => Task.CompletedTask;

        public Task Upload(string objectName, Stream readOnlyInput)
            => _s3.UploadObjectFromStreamAsync(_bucketName, objectName, readOnlyInput, null);
    }
}
