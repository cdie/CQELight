﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore.CosmosDb.Common;
using CQELight.EventStore.CosmosDb.Models;
using CQELight.EventStore.MongoDb.Models;
using CQELight.Tools.Extensions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace CQELight.EventStore.CosmosDb.Snapshots
{
    public class NumericSnapshotBehavior : ISnapshotBehavior
    {
        #region Members

        private readonly int _nbEvents;

        #endregion

        #region Ctor

        public NumericSnapshotBehavior(int nbEvents)
        {
            if (nbEvents < 2)
            {
                throw new ArgumentException("NumericSnapshotBehavior.ctor() : The number of events to generate" +
                    " a snapshot should be greater or equal to 2.");
            }
            _nbEvents = nbEvents;
        }

        #endregion

        #region ISnapshotBehavior methods

        public async Task<(ISnapshot Snapshot, int NewSequence)> GenerateSnapshotAsync(Guid aggregateId, Type aggregateType)
        {
            Snapshot snap = null;
            const int newSequence = 1;
            if (aggregateType.CreateInstance() is IEventSourcedAggregate aggregateInstance)
            {
                var events = EventStoreAzureDbContext.Client.CreateDocumentQuery<Event>(EventStoreAzureDbContext.DatabaseLink)
                  .Where(@event => @event.AggregateId == aggregateId && @event.AggregateType == aggregateType.AssemblyQualifiedName)
                  .Select(EventStoreManager.GetRehydratedEventFromDbEvent).ToList().AsEnumerable();


                aggregateInstance.RehydrateState(events);

                object stateProp =
                    aggregateType.GetAllProperties().FirstOrDefault(p => p.PropertyType.IsSubclassOf(typeof(AggregateState)))
                    ??
                    (object)aggregateType.GetAllFields().FirstOrDefault(f => f.FieldType.IsSubclassOf(typeof(AggregateState)));

                AggregateState state = null;
                if (stateProp is PropertyInfo propInfo)
                {
                    state = propInfo.GetValue(aggregateInstance) as AggregateState;
                }
                else if (stateProp is FieldInfo fieldInfo)
                {
                    state = fieldInfo.GetValue(aggregateInstance) as AggregateState;
                }

                if (state != null)
                {
                    snap = new Snapshot(
                      aggregateId: aggregateId,
                      aggregateType: aggregateType.AssemblyQualifiedName,
                      aggregateState: state,
                      snapshotBehaviorType: typeof(NumericSnapshotBehavior).AssemblyQualifiedName,
                      snapshotTime: DateTime.Now);

                    EventStoreAzureDbContext.Client.CreateDocumentQuery<Event>(EventStoreAzureDbContext.DatabaseLink)
                        .Where(@event => @event.AggregateId == aggregateId && @event.AggregateType == aggregateType.AssemblyQualifiedName)
                        .AsDocumentQuery().ExecuteNextAsync<Document>().GetAwaiter().GetResult()
                        .DoForEach(e => EventStoreAzureDbContext.Client.DeleteDocumentAsync(documentLink: e.SelfLink).GetAwaiter().GetResult());

                }
            }
            return (snap, newSequence);
        }

        public async Task<bool> IsSnapshotNeededAsync(Guid aggregateId, Type aggregateType)
        {
            return (await EventStoreAzureDbContext.Client.CreateDocumentQuery<Event>(EventStoreAzureDbContext.DatabaseLink)
                   .Where(@event => @event.AggregateId == aggregateId && @event.AggregateType == aggregateType.AssemblyQualifiedName).CountAsync().ConfigureAwait(false)) >= _nbEvents;
        }

        #endregion
    }
}
