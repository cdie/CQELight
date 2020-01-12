﻿using CQELight.Abstractions.DDD;
using System;

namespace CQELight.EventStore.MongoDb.Models
{
    internal class Snapshot
    {

        #region Properties

        public Guid Id { get; private set; }
        public AggregateState AggregateState { get; private set; }
        public string SnapshotBehaviorType { get; private set; }
        public DateTime SnapshotTime { get; private set; }
        public object AggregateId { get; private set; }
        public string AggregateType { get; private set; }

        #endregion

        #region Ctor

        internal Snapshot() { }

        public Snapshot(object aggregateId, Type aggregateType, AggregateState aggregateState, Type snapshotBehaviorType, DateTime snapshotTime)
            : this(Guid.NewGuid(), aggregateId, aggregateType, aggregateState, snapshotBehaviorType, snapshotTime)
        {
        }

        public Snapshot(Guid id, object aggregateId, Type aggregateType, AggregateState aggregateState, Type snapshotBehaviorType, DateTime snapshotTime)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(aggregateType));
            AggregateState = aggregateState ?? throw new ArgumentNullException(nameof(aggregateState));

            SnapshotBehaviorType = snapshotBehaviorType.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(snapshotBehaviorType));
            SnapshotTime = snapshotTime;

            Id = id;
        }

        #endregion

    }
}
