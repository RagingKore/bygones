namespace Bygones; 

[PublicAPI]
public abstract record EntityState<T>  where T : EntityState<T> {
    Dictionary<Type, Func<T, object, T>> Handlers { get; } = new();

    public T Apply(object domainEvent) {
        var type = domainEvent.GetType();

        if (Handlers.TryGetValue(type, out var handler))
            return handler!((T)this, domainEvent);

        if (HasFallbackType(type) && Handlers.TryGetValue(type.BaseType!, out var fallbackHandler))
            return fallbackHandler!((T)this, domainEvent);

        return (T)this;
        
        // checks if it can fallback to an abstract base type
        static bool HasFallbackType(Type eventType) => eventType.BaseType?.IsClass ?? false;
    }

    protected void On<TEvent>(Func<T, TEvent, T> onEvent) {
        if (!Handlers.TryAdd(typeof(TEvent), (state, evt) => onEvent(state, (TEvent)evt)))
            throw DuplicateStateHandlerException.For<T, TEvent>();
    }
}

[Serializable]
public class DuplicateStateHandlerException : InvalidOperationException {
    public DuplicateStateHandlerException(Type stateType, Type eventType) 
        : base($"Duplicate handler for {stateType.Name}: {eventType.Name}") { }
    
    public static DuplicateStateHandlerException For<T, TEvent>() => new DuplicateStateHandlerException(typeof(T), typeof(TEvent));

    protected DuplicateStateHandlerException(
        SerializationInfo info,
        StreamingContext context
    ) : base(info, context) { }
}

[PublicAPI]
public abstract class EventSourcedEntity {
    private protected List<object> Changes { get; } = new();

    public long   Version { get; private protected set; } = -1;
    public string Id      { get; private protected set; } = null!;
    
    public long   OriginalVersion => Version - Changes.Count;
    public bool   IsNew           => Version == -1;
    public bool   HasChanges      => Changes.Any();

    public IEnumerable<object> GetChanges() => Changes.ToArray();

    public IEnumerable<object> AcceptChanges() {
        foreach (var change in Changes)
            yield return change;

        Changes.Clear();
    }

    public abstract void Fold(object domainEvent);

    public override string ToString() => $"{GetType().Name}: {Id} v{Version}";

    public static T New<T>(string entityId) {
        return (T)Activator.CreateInstance(typeof(T), entityId)!;
    }
}

[PublicAPI]
public abstract class EventSourcedEntity<TState> : EventSourcedEntity where TState : EntityState<TState>, new() {
    public EventSourcedEntity() { }

    public EventSourcedEntity(string entityId) => Id = entityId;

    public EventSourcedEntity(TState state, long version) {
        State   = state;
        Version = version;
    }

    public TState State { get; private set; } = new();

    // public override void SetId(string entityId) =>
    //     State = State.SetId(entityId);

    public override void Fold(object domainEvent) {
        Version++;
        State = State.Apply(domainEvent);
    }

    protected internal (TState PreviousState, TState CurrentState, object[] changes) Apply(object domainEvent) {
        Changes.Add(domainEvent);
        var previous = State;
        State = State.Apply(domainEvent);
        return (previous, State, Changes.ToArray());
    }

    public override string ToString() => $"{GetType().Name}: {Id} v{Version}";
}