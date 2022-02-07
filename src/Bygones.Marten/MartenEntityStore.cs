namespace Elaway.Platform.EventSourcing.Marten;

[PublicAPI]
public class MartenEntityStore : IEntityStore {
    public MartenEntityStore(IDocumentStore store, ILogger<MartenEntityStore> logger) {
        Marten = store;
        Logger = logger;
    }

    IDocumentStore             Marten { get; }
    ILogger<MartenEntityStore> Logger { get; }

    public async Task<T> Load<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity, new() {
        if (string.IsNullOrWhiteSpace(entityId))
            throw new InvalidEntityId<T>();
        
        var stream = StreamKey.For<T>(entityId);
        var entity = EventSourcedEntity.New<T>(entityId);

        try {
            await using var session = Marten.LightweightSession();

            var events = session.Events
                .FetchStreamAsync(stream, token: cancellationToken)
                .ToAsyncEnumerable()
                .WithCancellation(cancellationToken);

            await foreach (var evt in events)
                entity.Fold(evt);
        }
        catch (Exception ex) {
            throw new EntityNotFound<T>(entityId, ex);
        }

        return entity;
    }

    public async Task<EntityStored> Save<T>(T entity, Metadata metadata, CancellationToken cancellationToken = default) where T : EventSourcedEntity {
        if (!entity.HasChanges)
            return null;

        var stream  = StreamKey.For<T>(entity.Id);
        var changes = entity.GetChanges().ToArray();

        await using var session = Marten.OpenSession();

        var action = session.Events.Append(stream, entity.OriginalVersion, changes);

        await session
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var lastEvent = await session.Events.QueryAllRawEvents()
            .Where(x => x.StreamKey == stream)
            .OrderByDescending(x => x.Sequence)
            .FirstAsync(cancellationToken);
        
        return new EntityStored(lastEvent.Version, (ulong?)lastEvent.Sequence, changes);
    }

    public async Task<bool> Exists<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity {
        var stream = StreamKey.For<T>(entityId).ToString();

        await using var session = Marten.LightweightSession();

        try {
            var streamState = await session.Events
                .FetchStreamStateAsync(stream, cancellationToken)
                .ConfigureAwait(false);
    
            return true;
        }
        catch (Exception ex) {
            return false;
        }
    }

    public async Task<bool> Forget<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity {
        var stream = StreamKey.For<T>(entityId).ToString();

        await using var session = Marten.LightweightSession();

        try {
            session.Events.ArchiveStream(stream);

            await session
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex) {
            return false;
        }
    }
}