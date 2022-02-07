namespace Bygones;

[PublicAPI]
public record StreamKey {
    public static readonly StreamKey Empty = new();

    string Value { get; init; } = String.Empty;
    
    public static StreamKey For(Type type, string entityId, string? tenantId = null) =>
        new StreamKey {
            Value = tenantId is not null 
                ? $"{tenantId}-{type.Name}-{entityId}" 
                : $"{type.Name}-{entityId}"
        };

    public static StreamKey For<T>(string entityId)                  => For(typeof(T), entityId);
    public static StreamKey For<T>(string tenantId, string entityId) => For(typeof(T), entityId, tenantId);
  
    public override string ToString() => Value;
    
    public static implicit operator string(StreamKey streamKey) => streamKey.Value;
}

public delegate StreamKey GetStreamKey(Type entityType, string entityId);