namespace Bygones.EventStore.Serializers;

public interface IEventSerializer {
    string ContentType { get; }

    object? Deserialize(ReadOnlyMemory<byte> data, string typeName);

    (string TypeName, ReadOnlyMemory<byte> Data) Serialize(object data);
}