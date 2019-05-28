using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using GridFSSyncService.Implementation;
using GridFSSyncService.Tests.Implementation;

using Xunit;

namespace GridFSSyncService.Tests
{
    public sealed class SynchronizerTests
    {
        private const int Seed = 1000;

        public static TheoryData Sources
            => new TheoryData<IEnumerable<ObjectInfo>, IEnumerable<ObjectInfo>>
            {
                {
                    Enumerable.Empty<ObjectInfo>(),
                    Enumerable.Empty<ObjectInfo>()
                },
                {
                    GenerateObjectInfos(Seed).Take(131730),
                    GenerateObjectInfos(Seed).Take(191920)
                },
            };

        [Theory]
        [MemberData(nameof(Sources))]
        internal void Test(IEnumerable<ObjectInfo> localInfos, IEnumerable<ObjectInfo> remoteInfos)
        {
            localInfos = localInfos.ToList();
            remoteInfos = remoteInfos.ToList();
            var writer = ActAndArrange(localInfos, remoteInfos);

            var deleted = remoteInfos.Select(info => info.Name).ToHashSet();
            deleted.ExceptWith(localInfos.Select(info => info.Name));
            Assert.True(deleted.SetEquals(writer.GetDeletes()));

            var uploaded = localInfos.Select(info => info.Name).ToHashSet();
            uploaded.ExceptWith(remoteInfos.Select(info => info.Name));
            Assert.True(uploaded.SetEquals(writer.GetUploads().Select(upload => upload.Name)));
        }

        private static ObjectWriterFake ActAndArrange(IEnumerable<ObjectInfo> localInfos, IEnumerable<ObjectInfo> remoteInfos)
        {
            var localSource = new ObjectSourceStub(localInfos);
            var reader = new ObjectReaderFake();
            var remoteSource = new ObjectSourceStub(remoteInfos);
            var writer = new ObjectWriterFake();
            var synchronizer = new Synchronizer(localSource, reader, remoteSource, writer);
            synchronizer.Synchronize().Wait();
            return writer;
        }

        private static IEnumerable<ObjectInfo> GenerateObjectInfos(int seed)
        {
            var rng1 = new Random(seed);
            var rng2 = new Random(seed + 1);
            while (true)
            {
                yield return new ObjectInfo(rng1.Next().ToString("x", CultureInfo.InvariantCulture) + "-" + rng2.Next().ToString("x", CultureInfo.InvariantCulture), rng1.Next());
            }
        }
    }
}
