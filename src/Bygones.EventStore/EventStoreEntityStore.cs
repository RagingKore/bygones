namespace Bygones.EventStore;

public class EventStoreEntityStore : IEntityStore {
    public EventStoreEntityStore(
        EventStoreClient client, IEventSerializer serializer, TypeMapper typeMapper, ILogger<EventStoreEntityStore> logger
    ) {
        EventStoreClient = client;
        Serializer  = serializer;
        Logger           = logger;
        TypeMapper       = typeMapper;
    }

    EventStoreClient               EventStoreClient { get; }
    IEventSerializer               Serializer       { get; }
    TypeMapper                     TypeMapper       { get; }
    ILogger<EventStoreEntityStore> Logger           { get; }

    public async Task<T> Load<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity, new() {
        if (string.IsNullOrWhiteSpace(entityId))
            throw new InvalidEntityId<T>();
        
        var stream = StreamKey.For<T>(entityId);
        var entity = EventSourcedEntity.New<T>(entityId);
        
        var readCount = 0L;
        var position  = StreamPosition.Start;

        try {
            await using var result = EventStoreClient
                .ReadStreamAsync(
                    Direction.Forwards,
                    stream,
                    position,
                    cancellationToken: cancellationToken
                );

            await foreach (var resolvedEvent in result) {
                var domainEvent = Serializer.Deserialize(
                    resolvedEvent.Event.Data,
                    resolvedEvent.Event.EventType
                );

                if (domainEvent is not null)
                    entity.Fold(
                        domainEvent
                    );

                readCount++;
            }

            return entity;
        }
        catch (StreamNotFoundException) {
            // _logger?.LogWarning("Stream {Stream} not found", stream);
            // throw new StreamNotFound(
            //     stream
            // );
        }

        catch (Exception ex) {
            //var (message, args) = getError();
            //_logger?.LogWarning(ex, message, args);
            // throw getException(
            //     stream,
            //     ex
            // );
        }

            // try {
        //     var position = StreamPosition.Start;
        //
        //     while (true) {
        //         var result = Client.ReadStreamAsync(
        //             Direction.Forwards,
        //             stream,
        //             position,
        //             pageSize,
        //             cancellationToken: cancellationToken
        //         );
        //
        //         var readCount = 0L;
        //
        //         await foreach (var resolvedEvent in result) {
        //
        //             var domainEvent = Deserialize(resolvedEvent.Event.EventType, resolvedEvent.Event.Data);
        //             //resolvedEvent.Event.Metadata.ToArray(),
        //
        //             entity.Fold(domainEvent);
        //             
        //             readCount++;
        //         }
        //
        //         if (readCount == 0)
        //             break;
        //
        //         position = position + readCount;
        //     }
        // }
        // catch (StreamNotFound e) {
        //     throw new Exceptions.EntityNotFound<T>(id, e);
        // }

        return entity;
    }
    
    public async Task<EntityStored> Save<T>(T entity, Metadata metadata, CancellationToken cancellationToken = default) where T : EventSourcedEntity {
        var stream           = StreamKey.For<T>(entity.Id);
        var expectedRevision = StreamRevision.FromInt64(entity.OriginalVersion);
        var changes          = entity.GetChanges().ToArray();
        var eventData        = changes.Select(ToEventData);

        // var result = await EventStoreClient.ConditionalAppendToStreamAsync(stream, expectedRevision, eventData, cancellationToken: cancellationToken);
        //
        // return result.Status switch {
        //     ConditionalWriteStatus.Succeeded => new EntityStored(
        //         result.NextExpectedStreamRevision.ToInt64(),
        //         result.LogPosition.CommitPosition,
        //         changes
        //     ),
        //     ConditionalWriteStatus.VersionMismatch => throw new InvalidOperationException(ConditionalWriteStatus.VersionMismatch.ToString()),
        //     ConditionalWriteStatus.StreamDeleted   => throw new InvalidOperationException(ConditionalWriteStatus.StreamDeleted.ToString()),
        // };

        var result = await EventStoreClient.AppendToStreamAsync(
            stream,
            expectedRevision,
            eventData,
            cancellationToken: cancellationToken
        );

        entity.AcceptChanges();
        
        return new EntityStored(
            result.NextExpectedStreamRevision.ToInt64(),
            result.LogPosition.CommitPosition,
            changes
        );

        EventData ToEventData(object domainEvent) =>
            new(
                Uuid.NewUuid(),
                domainEvent.GetType().Name,
                Serializer.Serialize(domainEvent).Data, 
                ReadOnlyMemory<byte>.Empty
            );
    }

    public async Task<bool> Exists<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity {
        var stream = StreamKey.For<T>(entityId);
        
        var read = EventStoreClient.ReadStreamAsync(
            Direction.Backwards,
            stream,
            StreamPosition.End,
            1,
            cancellationToken: cancellationToken
        );

        return await read.ReadState.ContinueWith(
            x => x.IsCompletedSuccessfully && x.Result == ReadState.Ok,
            cancellationToken
        );
    }
    
    public async Task<bool> Forget<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity {
        var stream = StreamKey.For<T>(
            entityId
        );

        try {
            var read = await EventStoreClient.TombstoneAsync(
                stream,
                StreamState.StreamExists,
                cancellationToken: cancellationToken
            );

            return true;// ReadState.Ok;
        }
        catch (StreamNotFoundException) {
            return false; //ReadState.StreamNotFound;
        }
       
    }
}

// public enum ReadState {
//     /// <summary>The stream does not exist.</summary>
//     StreamNotFound,
//
//     /// <summary>The stream exists.</summary>
//     Ok,
// }


// public class EntityStore : IEntityStore
// {
//    // static readonly ILog Log = LogProvider.For<EntityStore>();
//
//     readonly IEventStore   _eventStore;
//     readonly GetStreamName _getStreamName;
//
//     public EntityStore(IEventStore eventStore, ILogger<EntityStore> log, GetStreamName getStreamName = null) {
//         _eventStore    = Ensure.NotNull(eventStore, nameof(eventStore));
//         _getStreamName = getStreamName ?? StreamKey.For;
//         Log            = log;
//     }
//
//     public ILogger<EntityStore> Log { get; set; }
//
//     /// <inheritdoc />
//     public async Task<T> Load<T>(string entityId, CancellationToken cancellationToken = default) where T : class, IEventSourcedEntity, new() {
//         Ensure.NotNullOrWhiteSpace(entityId, nameof(entityId));
//
//         var stream        = _getStreamName(typeof(T), entityId);
//         var entity        = new T();
//         var nextPageStart = 0L;
//
//         do {
//             var page = await _eventStore.ReadStream(stream, nextPageStart, StreamPageSize.Max, StreamReadDirection.Forward, cancellationToken);
//
//             if (page.Events.Any())
//                 entity.Restore(
//                     page.Events.Last().Version,
//                     page.Events.Select(e => e.Data)
//                 );
//
//             nextPageStart = !page.IsEndOfStream ? page.NextVersion : -1;
//         } while (nextPageStart != -1);
//
//         entity.SetId(entityId);
//
//         Log.Debug(
//             entity.Version == -1
//                 ? "{Id} v{Version} created from stream {stream}"
//                 : "{Id} v{Version} loaded from stream {stream}",
//             entityId, entity.Version, stream
//         );
//
//         return entity;
//     }
//
//     /// <inheritdoc />
//     public async Task<(long NextExpectedVersion, ulong? StreamPosition, StoredEvent[] Events)> Save<T>(
//         T entity,
//         Metadata metadata,
//         CancellationToken cancellationToken = default
//     ) where T : class, IEventSourcedEntity {
//         Ensure.NotNull(entity, nameof(entity));
//
//         if (!entity.HasChanges) {
//             //Log.Debug("{entity} has no changes", entity);
//
//             return (entity.Version, null, new StoredEvent[0]);
//         }
//
//         Metadata GetEventMetadata(int idx)
//             => new Metadata(metadata).AddRange(
//                 (MetadataKeys.EventSource, typeof(T).FullName),
//                 (MetadataKeys.EventSourceId, entity.Id),
//                 (MetadataKeys.EventSourceVersion, (entity.Version + idx + 1).ToString())
//             );
//
//         var changes = entity
//             .GetChanges()
//             .Select((evt, idx) => new StreamEvent(evt, GetEventMetadata(idx)))
//             .ToArray();
//
//         var stream = _getStreamName(typeof(T), entity.Id);
//
//         var result = await _eventStore.AppendStream(stream, entity.Version, changes, cancellationToken);
//
//         entity.AcceptChanges();
//
//         var storedEvents = changes
//             .Select(
//                 evt => {
//                     Log.Debug(evt.ToString);
//
//                     return new StoredEvent(evt.Data, evt.Metadata);
//                 }
//             )
//             .ToArray();
//
//         Log.Debug(
//             entity.Version == -1
//                 ? "{entity} changes appended to new stream {stream}"
//                 : "{entity} changes appended to stream {stream}",
//             entity, stream
//         );
//
//         return (result.NextExpectedVersion, result.StreamPosition, storedEvents);
//     }
// }