﻿using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.EventStore.Attributes;
using CQELight.EventStore.CosmosDb.Common;
using CQELight.EventStore.CosmosDb.Models;
using CQELight.Tools;
using CQELight.Tools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CQELight.EventStore.CosmosDb
{
    internal class CosmosDbEventStore : IEventStore
    {

        #region Private members

        private readonly ISnapshotBehaviorProvider _snapshotBehaviorProvider;

        #endregion

        #region Ctor

        public CosmosDbEventStore(ISnapshotBehaviorProvider snapshotBehaviorProvider = null)
        {
            _snapshotBehaviorProvider = snapshotBehaviorProvider;
        }

        #endregion

        #region IEventStore methods

        /// <summary>
        /// Get a collection of events for a specific aggregate.
        /// </summary>
        /// <param name="aggregateUniqueId">Id of the aggregate which we want all the events.</param>
        /// <typeparam name="TAggregate">Aggregate type.</typeparam>
        /// <returns>Collection of all associated events.</returns>
        public Task<IEnumerable<IDomainEvent>> GetEventsFromAggregateIdAsync<TAggregate>(Guid aggregateUniqueId)
            where TAggregate : class
            => GetEventsFromAggregateIdAsync(aggregateUniqueId, typeof(TAggregate));

        /// <summary>
        /// Store a domain event in the event store
        /// </summary>
        /// <param name="event">Event instance to be persisted.</param>
        public async Task StoreDomainEventAsync(IDomainEvent @event)
        {
            var eventType = @event.GetType();
            if (eventType.IsDefined(typeof(EventNotPersistedAttribute)))
            {
                return;
            }

            ISnapshotBehavior behavior = _snapshotBehaviorProvider?.GetBehaviorForEventType(eventType);
            if (behavior != null && await behavior.IsSnapshotNeededAsync(@event.AggregateId.Value, @event.AggregateType).ConfigureAwait(false))
            {
                var result = await behavior.GenerateSnapshotAsync(@event.AggregateId.Value, @event.AggregateType).ConfigureAwait(false);
                if (result.Snapshot != null)
                {
                    await EventStoreAzureDbContext.Client.CreateDocumentAsync(EventStoreAzureDbContext.DatabaseLink, result.Snapshot).ConfigureAwait(false);
                }
            }

            await SaveEvent(@event).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an event per its id.
        /// </summary>
        /// <param name="eventId">Id of the event.</param>
        /// <typeparam name="TEvent">Type of event to retrieve.</typeparam>
        /// <returns>Instance of the event.</returns>
        public Task<TEvent> GetEventByIdAsync<TEvent>(Guid eventId)
            where TEvent : class, IDomainEvent
            => Task.Run(()
                => EventStoreManager.GetRehydratedEventFromDbEvent(
                    EventStoreAzureDbContext.Client.CreateDocumentQuery<Event>(EventStoreAzureDbContext.DatabaseLink)
                    .Where(@event => @event.Id == eventId).ToList().FirstOrDefault()) as TEvent);

        /// <summary>
        /// Get a collection of events for a specific aggregate.
        /// </summary>
        /// <param name="aggregateUniqueId">Id of the aggregate which we want all the events.</param>
        /// <param name="aggregateType">Type of the aggregate.</param>
        /// <returns>Collection of all associated events.</returns>
        public Task<IEnumerable<IDomainEvent>> GetEventsFromAggregateIdAsync(Guid aggregateUniqueId, Type aggregateType)
            => Task.Run(() => EventStoreAzureDbContext.Client.CreateDocumentQuery<Event>(EventStoreAzureDbContext.DatabaseLink)
                  .Where(@event => @event.AggregateId == aggregateUniqueId && @event.AggregateType == aggregateType.AssemblyQualifiedName)
                  .ToList().Select(x => EventStoreManager.GetRehydratedEventFromDbEvent(x)).ToList().AsEnumerable());


        #endregion        

        #region Private methods

        

        private Task SaveEvent(IDomainEvent @event)
        {
            var persistedEvent = new Event
            {
                AggregateId = @event.AggregateId,
                AggregateType = @event.AggregateType?.AssemblyQualifiedName,
                EventData = @event.ToJson(),
                EventTime = @event.EventTime,
                Id = @event.Id,
                Sequence = @event.Sequence,
                EventType = @event.GetType().AssemblyQualifiedName
            };
            return EventStoreAzureDbContext.Client.CreateDocumentAsync(EventStoreAzureDbContext.DatabaseLink, persistedEvent);
        }

        #endregion

    }
}
