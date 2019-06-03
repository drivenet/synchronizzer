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
                    GenerateObjectInfos(Seed, 171),
                    GenerateObjectInfos(Seed, 313)
                },
                {
                    GenerateObjectInfos(Seed, 313),
                    GenerateObjectInfos(Seed, 171)
                },
                {
                    GenerateObjectInfos(Seed, 171),
                    GenerateObjectInfos(Seed + 1, 313)
                },
                {
                    GenerateObjectInfos(Seed, 313),
                    GenerateObjectInfos(Seed + 1, 171)
                },
                {
                    GenerateObjectInfos(Seed, 3000),
                    GenerateObjectInfos(Seed, 4000)
                },
                {
                    GenerateObjectInfos(Seed, 317303),
                    GenerateObjectInfos(Seed, 919201)
                },
                {
                    GenerateObjectInfos(Seed, 919201),
                    GenerateObjectInfos(Seed, 317303)
                },
                {
                    GenerateObjectInfos(Seed, 317303),
                    GenerateObjectInfos(Seed + 1, 919201)
                },
                {
                    GenerateObjectInfos(Seed, 919201),
                    GenerateObjectInfos(Seed + 1, 317303)
                },
            };

        [Theory]
        [MemberData(nameof(Sources))]
        internal void Test(IEnumerable<ObjectInfo> localInfos, IEnumerable<ObjectInfo> remoteInfos)
        {
            localInfos = localInfos.ToList();
            remoteInfos = remoteInfos.ToList();
            var localSource = new ObjectSourceStub(localInfos);
            var reader = new ObjectReaderMock();
            var remoteSource = new ObjectSourceStub(remoteInfos);
            var writer = new ObjectWriterMock();
            var synchronizer = new Synchronizer(localSource, reader, remoteSource, writer);
            synchronizer.Synchronize().GetAwaiter().GetResult();

            var deleted = remoteInfos.Select(info => info.Name).ToHashSet();
            var uploaded = localInfos
                .Where(info => !deleted.Contains(info.Name))
                .Select(info => (info.Name, reader.GetStream(info.Name)))
                .ToHashSet();
            deleted.ExceptWith(localInfos.Select(info => info.Name));
            Assert.True(deleted.SetEquals(writer.GetDeletes()));
            Assert.True(uploaded.SetEquals(writer.GetUploads()));
        }

        private static IEnumerable<ObjectInfo> GenerateObjectInfos(int seed, int count)
        {
            var rng1 = new Random(seed);
            var rng2 = new Random(seed + 1);
            for (var i = 0; i < count; i++)
            {
                yield return new ObjectInfo(rng1.Next().ToString("x", CultureInfo.InvariantCulture) + "-" + rng2.Next().ToString("x", CultureInfo.InvariantCulture), rng1.Next());
            }
        }
    }
}
