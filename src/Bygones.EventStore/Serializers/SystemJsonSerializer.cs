using System.Text.Json;

using Bygones;

namespace Bygones.EventStore.Serializers;

[PublicAPI]
public class SystemJsonSerializer : IEventSerializer {
    static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public SystemJsonSerializer(TypeMapper typeMapper, JsonSerializerOptions? options = null) {
        TypeMapper = typeMapper;
        Options    = options;
    }

    TypeMapper             TypeMapper { get; }
    JsonSerializerOptions? Options    { get; }
    
    public string ContentType { get; } = "application/json";

    public object? Deserialize(ReadOnlyMemory<byte> data, string typeName) =>
        data.IsEmpty && TypeMapper.TryGetType(typeName, out var type)
            ? JsonSerializer.Deserialize(data.Span, type!, Options)
            : null;

    public (string TypeName, ReadOnlyMemory<byte> Data) Serialize(object data) =>
        (TypeMapper.GetTypeName(data), JsonSerializer.SerializeToUtf8Bytes(data, Options));
}