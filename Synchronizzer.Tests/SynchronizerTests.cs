using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Synchronizzer.Implementation;
using Synchronizzer.Tests.Implementation;

using Xunit;

namespace Synchronizzer.Tests
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
                    Enumerable.Empty<ObjectInfo>()
                },
                {
                    Enumerable.Empty<ObjectInfo>(),
                    GenerateObjectInfos(Seed, 313)
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
                    Enumerable.Empty<ObjectInfo>()
                },
                {
                    Enumerable.Empty<ObjectInfo>(),
                    GenerateObjectInfos(Seed, 4000)
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
        internal async Task Test(IEnumerable<ObjectInfo> originInfos, IEnumerable<ObjectInfo> destinationInfos)
        {
            originInfos = originInfos.ToList();
            destinationInfos = destinationInfos.ToList();
            var originSource = new ObjectSourceStub(originInfos);
            var reader = new ObjectReaderMock();
            var destinationSource = new ObjectSourceStub(destinationInfos);
            var writer = new ObjectWriterMock();
            using var originReader = new OriginReader("", originSource, reader, null);
            var locker = new ObjectWriterLockerStub();
            using var destinationWriter = new DestinationWriter("", destinationSource, writer, locker, null);
            var taskManager = new QueuingTaskManager(new FixedQueuingSettings());
            using var synchronizer = new Synchronizer(originReader, destinationWriter, taskManager, false, false, false, null, null);
            await synchronizer.Synchronize(default);

            var deleted = destinationInfos
                .Where(info => !info.IsHidden)
                .Select(info => info.Name)
                .ToHashSet();
            var uploaded = originInfos
                .Where(info => !info.IsHidden && !deleted.Contains(info.Name))
                .Select(info => (info.Name, reader.GetStream(info.Name)))
                .ToHashSet();
            deleted.ExceptWith(originInfos.Select(info => info.Name));
            Assert.True(deleted.SetEquals(writer.GetDeletes()));
            Assert.True(uploaded.SetEquals(writer.GetUploads()));
        }

        private static IEnumerable<ObjectInfo> GenerateObjectInfos(int seed, int count)
        {
            var rng1 = new Random(seed);
            var rng2 = new Random(seed + 1);
            for (var i = 0; i < count; i++)
            {
                yield return new ObjectInfo(
                    rng1.Next().ToString("x", CultureInfo.InvariantCulture) + "-" + rng2.Next().ToString("x", CultureInfo.InvariantCulture),
                    rng1.Next(),
                    rng1.NextDouble() < 0.1,
                    new DateTime(0, DateTimeKind.Utc),
                    null);
            }
        }
    }
}
