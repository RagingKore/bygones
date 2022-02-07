namespace Bygones;

// public abstract class Identity<T, TId> : ValueObject<T> where T : Identity<T, TId>, new() {
//     public static readonly T Default = CreateIdentity(default);
//
//     public Identity() { }
//
//     protected Identity(TId value) => Value = value;
//
//     public TId Value { get; protected set; }
//
//     protected static Func<object, (TId Id, Error[] Errors)> ParseValue { get; set; }
//
//     public static (T Id, Error[] Errors) Parse(object value) {
//         if (ParseValue is null)
//             return TryConvertValue(value, out var id)
//                 ? (CreateIdentity(id), Empty<Error>())
//                 : (Default, new[] {
//                     new Error(typeof(T).Name, $"{value} is not a valid {typeof(T).Name}")
//                 });
//
//         var result = ParseValue(value);
//
//         return result.Errors.Any()
//             ? (Default, new[] {
//                 new Error(typeof(T).Name, $"{value} is not a valid {typeof(T).Name}")
//             })
//             : (CreateIdentity(result.Id), Empty<Error>());
//
//         static bool TryConvertValue(object rawValue, out TId value) {
//             try {
//                 var underlyingType = Nullable.GetUnderlyingType(typeof(TId));
//
//                 value = (TId)Convert.ChangeType(rawValue, underlyingType ?? typeof(TId));
//
//                 return true;
//             }
//             catch {
//                 value = default;
//
//                 return false;
//             }
//         }
//     }
//
//     public static T From(object value) {
//         var (id, errors) = Parse(value);
//
//         return errors.Any()
//             ? throw new InvalidIdentity<T>(value, ParseValue is null
//                                                ? Empty<Error>()
//                                                : errors)
//             : id;
//     }
//
//     /// <inheritdoc />
//     public override string ToString() => Value.ToString();
//
//     public static implicit operator string(Identity<T, TId> self) => self.ToString();
//     public static implicit operator TId(Identity<T, TId> self)    => self.Value;
//     public static implicit operator Identity<T, TId>(TId value)   => CreateIdentity(value);
//
//     static T CreateIdentity(TId value) =>
//         new() {
//             Value = value
//         };
// }
//
// public class InvalidIdentity<T> : DomainException {
//     public InvalidIdentity(object value, params Error[] errors)
//         : base($"{value} is not a valid {typeof(T).Name}", errors) { }
// }
//
// public class InvalidValue<T> : DomainException {
//     public InvalidValue(params Error[] errors)
//         : base($"{typeof(T).Name} is invalid", errors) { }
// }