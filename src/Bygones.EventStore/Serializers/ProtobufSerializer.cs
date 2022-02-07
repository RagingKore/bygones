using System.Collections.Concurrent;
using System.Reflection;

using Bygones;

using Google.Protobuf;

namespace Bygones.EventStore.Serializers;

public class ProtobufSerializer : IEventSerializer {
    static readonly ConcurrentDictionary<Type, MessageParser> Parsers = new();

    public ProtobufSerializer(TypeMapper typeMapper) => TypeMapper = typeMapper;

    TypeMapper TypeMapper { get; }

    public string ContentType => "application/protobuf";

    public (string TypeName, ReadOnlyMemory<byte> Data) Serialize(object? data) {
        var message  = (IMessage)data!;

        var typeName = TypeMapper.GetTypeName(data);
        
        return data is not null
            ? (typeName, ((IMessage)data).ToByteArray())
            :(typeName, ReadOnlyMemory<byte>.Empty);
    }

    public object? Deserialize(ReadOnlyMemory<byte> data, string typeName) {
        if (data.IsEmpty)
            return default;
        
        TypeMapper.TryGetType(typeName, out var type);

        return Parsers
            .GetOrAdd(type, GetParser)
            .ParseFrom(data.ToArray());

        //get static instance
        static MessageParser GetParser(Type type) =>
            (type.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null, null) as MessageParser)!;
    }
}