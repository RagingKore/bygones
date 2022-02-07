namespace Bygones;

[PublicAPI]
public class Metadata : Dictionary<string, string> {
    public Metadata() { }

    public Metadata(IDictionary<string, string> dictionary) : base(dictionary) { }

    public Metadata(IReadOnlyDictionary<string, string> dictionary) : base(dictionary) { }
}