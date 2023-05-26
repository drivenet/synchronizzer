using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Runtime;
using Amazon.S3;

namespace Synchronizzer.Implementation
{
    internal sealed class ExceptionHandlingS3Mediator : IS3Mediator
    {
        private readonly IS3Mediator _inner;

        public ExceptionHandlingS3Mediator(IS3Mediator inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public Uri ServiceUrl => _inner.ServiceUrl;

        public async Task<TResult> Invoke<TResult>(Func<IAmazonS3, CancellationToken, Task<TResult>> action, FormattableString description, CancellationToken cancellationToken)
        {
            try
            {
                return await _inner.Invoke(action, description, cancellationToken);
            }
            catch (AmazonServiceException exception) when (exception.InnerException is WebException webException && webException.Status == WebExceptionStatus.RequestCanceled)
            {
                throw new OperationCanceledException("Amazon service exception treated as request cancellation.", exception);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                throw new OperationCanceledException("S3 \"service unavailable\" exception treated as request cancellation.", exception);
            }
        }

        public async Task Cleanup(Func<IAmazonS3, Task> action, FormattableString description)
        {
            try
            {
                await _inner.Cleanup(action, description);
            }
            catch (AmazonServiceException exception) when (exception.InnerException is WebException webException && webException.Status == WebExceptionStatus.RequestCanceled)
            {
                throw new OperationCanceledException("Amazon service exception treated as cleanup cancellation.", exception);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                throw new OperationCanceledException("S3 \"service unavailable\" exception treated as cleanup cancellation.", exception);
            }
        }
    }
}
