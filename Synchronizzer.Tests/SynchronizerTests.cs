using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
        internal void Test(IEnumerable<ObjectInfo> originInfos, IEnumerable<ObjectInfo> destinationInfos)
        {
            originInfos = originInfos.ToList();
            destinationInfos = destinationInfos.ToList();
            var originSource = new ObjectSourceStub(originInfos);
            var reader = new ObjectReaderMock();
            var destinationSource = new ObjectSourceStub(destinationInfos);
            var writer = new ObjectWriterMock();
            var originReader = new OriginReader(originSource, reader);
            var locker = new ObjectWriterLockerStub();
            var destinationWriter = new DestinationWriter("", destinationSource, writer, locker);
            var taskManager = new QueuingTaskManager(new FixedQueuingSettings());
            var synchronizer = new Synchronizer(originReader, destinationWriter, taskManager, null);
            synchronizer.Synchronize(default).GetAwaiter().GetResult();

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
#pragma warning disable CA5394 // Do not use insecure randomness -- security is not needed
            var rng1 = new Random(seed);
            var rng2 = new Random(seed + 1);
            for (var i = 0; i < count; i++)
            {
                yield return new ObjectInfo(
                    rng1.Next().ToString("x", CultureInfo.InvariantCulture) + "-" + rng2.Next().ToString("x", CultureInfo.InvariantCulture),
                    rng1.Next(),
                    rng1.NextDouble() < 0.1);
            }
#pragma warning restore CA5394 // Do not use insecure randomness
        }
    }
}
