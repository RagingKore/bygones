namespace Bygones;

public record EntityStored(long NextExpectedVersion, ulong? StreamPosition, params object[] Events);

public interface IEntityStore {
    /// <summary>
    /// Loads an entity by id.
    /// </summary>
    Task<T> Load<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity, new();

    /// <summary>
    /// Persists a given entity's changes.
    /// </summary>
    Task<EntityStored> Save<T>(T entity, Metadata metadata, CancellationToken cancellationToken = default) where T : EventSourcedEntity;

    /// <summary>
    /// Checks if an entity exists.
    /// </summary>
    Task<bool> Exists<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity;

    /// <summary>
    /// Tombstones an entity. Note: Tombstoned/forgotten entities can never be recovered.
    /// </summary>
    Task<bool> Forget<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity;
    
    // /// <summary>
    // /// Archives an entity.id
    // /// </summary>
    // Task<bool> Archive<T>(string entityId, CancellationToken cancellationToken = default) where T : EventSourcedEntity;
}

// public record ApplyContext<TEntity>(TEntity Entity, Metadata Metadata, CancellationToken CancellationToken);

public static class EntityStoreExtensions {
    // public static async Task<EntityStored> Apply<TEntity>(
    //     this IEntityStore store, string entityId, Func<ApplyContext<TEntity>, Task> handler, CancellationToken cancellationToken
    // ) where TEntity : EventSourcedEntity, new() {
    //     var entity = await store
    //         .Load<TEntity>(entityId, cancellationToken)
    //         .ConfigureAwait(false);
    //
    //     var ctx = new ApplyContext<TEntity>(entity, new Metadata(), cancellationToken);
    //
    //     await handler(ctx).ConfigureAwait(false);
    //
    //     return await store
    //         .Save(entity, ctx.Metadata, cancellationToken)
    //         .ConfigureAwait(false);
    // }
    //
    public static async Task<EntityStored> Apply<TEntity>(
        this IEntityStore store, string entityId, Func<TEntity, Metadata, Task> handler, CancellationToken cancellationToken
    ) where TEntity : EventSourcedEntity, new() {

        var entity = await store
            .Load<TEntity>(entityId, cancellationToken)
            .ConfigureAwait(false);

        var metadata = new Metadata();
        
        await handler(entity, metadata).ConfigureAwait(false);

        return await store
            .Save(entity, metadata, cancellationToken)
            .ConfigureAwait(false);
    }

    public static Task<EntityStored> Apply<TEntity>(this IEntityStore store, string entityId, Func<TEntity, Task> handler, CancellationToken cancellationToken)
        where TEntity : EventSourcedEntity, new() =>
        Apply<TEntity>(
            store,
            entityId,
            (entity, _) => handler(entity),
            cancellationToken
        );
    //
    // public static Task<EntityStored> Apply<TEntity>(
    //     this IEntityStore store, string entityId, Action<TEntity, Metadata> handler, CancellationToken cancellationToken
    // ) where TEntity : EventSourcedEntity, new() =>
    //     Apply<TEntity>(
    //         store,
    //         entityId,
    //         (entity, metadata) => {
    //             handler(entity, metadata);
    //             return Task.CompletedTask;
    //         },
    //         cancellationToken
    //     );
    //
    // public static Task<EntityStored> Apply<TEntity>(this IEntityStore store, string entityId, Action<TEntity> handler, CancellationToken cancellationToken)
    //     where TEntity : EventSourcedEntity, new() =>
    //     Apply<TEntity>(
    //         store,
    //         entityId,
    //         (entity, _) => {
    //             handler(entity);
    //             return Task.CompletedTask;
    //         },
    //         cancellationToken
    //     );
}