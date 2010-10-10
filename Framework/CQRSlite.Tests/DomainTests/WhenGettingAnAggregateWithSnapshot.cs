﻿using System;
using CQRSlite.Domain;
using CQRSlite.Tests.TestSubstitutes;
using Xunit;

namespace CQRSlite.Tests.DomainTests
{
    public class WhenGettingAnAggregateWithSnapshot
    {
        private TestSnapshotStore _snapshotStore;
        private TestSnapshotAggreagate _aggregate;

        public WhenGettingAnAggregateWithSnapshot()
        {
            var eventStore = new TestEventStore();
            _snapshotStore = new TestSnapshotStore();
            var rep = new Repository<TestSnapshotAggreagate>(eventStore, _snapshotStore);
            _aggregate = rep.GetById(Guid.NewGuid());
        }

        [Fact]
        public void ShouldRestore()
        {
            Assert.True(_aggregate.Restored);
        }

        [Fact]
        public void ShouldLoadVersion()
        {
            Assert.Equal(2, _aggregate.Version);
        }
    }
}