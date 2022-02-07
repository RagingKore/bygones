using Bygones.Extensions;

namespace Bygones; 

public class EntityList<T, TState> : IEnumerable<T> where T : EventSourcedEntity<TState>, new() where TState : EntityState<TState>, new() {
    Dictionary<string, T> Index { get; } = new();

    public T this[string entityId] => Index[entityId];

    public void AddOrApply<TEvent>(string entityId, [DisallowNull] TEvent domainEvent) {
        if (domainEvent is null)
            throw new ArgumentNullException(nameof(domainEvent));

        Index[entityId] = Index.ContainsKey(entityId)
            ? Index[entityId].With(entity => entity.Apply(domainEvent))
            : EventSourcedEntity
                .New<T>(entityId)
                .With(x => x.Apply(domainEvent));
    }

    public void Add(T entity)           => Index.Add(entity.Id, entity);
    public void Remove(string entityId) => Index.Remove(entityId);
    public bool Exists(string entityId) => Index.ContainsKey(entityId);

    public IQueryable<T> AsQueryable() => Index.Values.AsQueryable();
    
    public IEnumerator<T> GetEnumerator() => Index.Values.GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}