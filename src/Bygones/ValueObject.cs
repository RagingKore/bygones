

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Bygones; 

// public abstract class ValueObject<T> where T : ValueObject<T> {
//     static readonly Member[] Members = GetMembers().ToArray();
//
//     public override bool Equals(object other) {
//         if (ReferenceEquals(null, other))
//             return false;
//
//         if (ReferenceEquals(this, other))
//             return true;
//
//         return other.GetType() == typeof(T)
//             && Members.All(m => {
//                    var otherValue = m.GetValue(other);
//                    var thisValue  = m.GetValue(this);
//
//                    return m.IsNonStringEnumerable
//                        ? GetCollectionElements(otherValue).SequenceEqual(GetCollectionElements(thisValue))
//                        : otherValue?.Equals(thisValue) ?? thisValue == null;
//                });
//     }
//
//     public override int GetHashCode() =>
//         CombineHashCodes(Members.Select(m => m.IsNonStringEnumerable
//                                             ? CombineHashCodes(GetCollectionElements(m.GetValue(this)))
//                                             : m.GetValue(this)));
//
//     public static bool operator ==(ValueObject<T> left, ValueObject<T> right) => Equals(left, right);
//     public static bool operator !=(ValueObject<T> left, ValueObject<T> right) => !Equals(left, right);
//
//     // public override string ToString() {
//     //     if (Members.Length == 1) {
//     //         var m     = Members[0];
//     //         var value = m.GetValue(this);
//     //
//     //         return m.IsNonStringEnumerable
//     //             ? $"{string.Join("|", GetCollectionElements(value))}"
//     //             : value.ToString();
//     //     }
//     //
//     //     var values = Members.Select(m => {
//     //         var value = m.GetValue(this);
//     //
//     //         return m.IsNonStringEnumerable
//     //             ? $"{m.Name}:{string.Join("|", GetCollectionElements(value))}"
//     //             : m.Type != typeof(string)
//     //                 ? $"{m.Name}:{value}"
//     //                 : value == null
//     //                     ? $"{m.Name}:null"
//     //                     : $"{m.Name}:\"{value}\"";
//     //     });
//     //
//     //     return $"{typeof(T).Name}[{string.Join("|", values)}]";
//     // }
//
//     static IEnumerable<Member> GetMembers() {
//         var valueObjectType = typeof(T);
//         
//         const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
//
//         while (valueObjectType != typeof(object)) {
//             if (valueObjectType is null)
//                 continue;
//
//             foreach (var p in valueObjectType.GetProperties(flags))
//                 yield return new Member(p);
//
//             foreach (var f in valueObjectType.GetFields(flags))
//                 yield return new Member(f);
//
//             valueObjectType = valueObjectType.BaseType;
//         }
//     }
//
//     static IEnumerable<object> GetCollectionElements(object obj) {
//         var enumerator = ((IEnumerable)obj).GetEnumerator();
//         while (enumerator.MoveNext())
//             yield return enumerator.Current!;
//     }
//
//     static int CombineHashCodes(IEnumerable<object> objs) {
//         unchecked {
//             return objs.Aggregate(17, (current, obj) => current * 59 + (obj?.GetHashCode() ?? 0));
//         }
//     }
//
//     record struct Member {
//         public string               Name                  { get; }
//         public Func<object, object> GetValue              { get; }
//         public bool                 IsNonStringEnumerable { get; }
//         public Type                 Type                  { get; }
//
//         public Member(MemberInfo info) {
//             switch (info) {
//                 case FieldInfo field:
//                     Name                  = field.Name;
//                     GetValue              = field.GetValue!;
//                     IsNonStringEnumerable = TypeIsNonStringEnumerable(field.FieldType);
//                     Type                  = field.FieldType;
//
//                     break;
//
//                 case PropertyInfo prop:
//                     Name                  = prop.Name;
//                     GetValue              = prop.GetValue!;
//                     IsNonStringEnumerable = TypeIsNonStringEnumerable(prop.PropertyType);
//                     Type                  = prop.PropertyType;
//
//                     break;
//
//                 default: throw new ArgumentException("Member is not a field or property?!", info.Name);
//             }
//
//             bool TypeIsNonStringEnumerable(Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
//         }
//         
//     }
// }


public abstract class ValueObjectClean<T> where T : ValueObjectClean<T> {
    static readonly TypeMembers.Member[] Members = TypeMembers.Get(typeof(T)).ToArray();
    
    int? CachedHashCode { get; set; }
    
    protected virtual IEnumerable<object?> GetEqualityMembers() {
        foreach (var member in Members) {
            if (member.IsNonStringEnumerable) {
                var enumerator = ((IEnumerable)member.GetValue(this)).GetEnumerator();
                while (enumerator.MoveNext())
                    yield return enumerator.Current!;
            }
            else {
                yield return member.GetValue(this);
            }
        }
    }

    // protected virtual IEnumerable<object?> GetEqualityMembers() {
    //     foreach (var member in Members) {
    //         if (member.IsNonStringEnumerable) {
    //             var enumerator = ((IEnumerable)member.GetValue(this)).GetEnumerator();
    //             while (enumerator.MoveNext())
    //                 yield return enumerator.Current!;
    //         }
    //         else {
    //             yield return member.GetValue(this);
    //         }
    //     }
    // }

    public bool Equals(ValueObjectClean<T>? other) {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;
        
        return GetEqualityMembers().SequenceEqual(other.GetEqualityMembers());
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((ValueObjectClean<T>)obj);
    }
    
    public override int GetHashCode() {
        if (CachedHashCode is null) {
            var hashCode = new HashCode();

            foreach (var obj in GetEqualityMembers())
                hashCode.Add(obj);

            CachedHashCode = hashCode.ToHashCode();
        }

        return CachedHashCode.Value;
    }

    public static bool operator ==(ValueObjectClean<T>? left, ValueObjectClean<T>? right) => Equals(left, right);
    public static bool operator !=(ValueObjectClean<T>? left, ValueObjectClean<T>? right) => !Equals(left, right);
    
    static class TypeMembers {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

        public static IEnumerable<Member> Get(Type type) {
            var baseType   = type;
            var objectType = typeof(object);

            while (baseType != objectType) {
                if (baseType is null)
                    continue;

                foreach (var p in baseType.GetProperties(Flags))
                    yield return Member.For(p);

                foreach (var f in baseType.GetFields(Flags))
                    yield return Member.For(f);

                baseType = baseType.BaseType;
            }
        }

        public record struct Member {
            public string               Name                  { get; init; }
            public Func<object, object> GetValue              { get; init; }
            public bool                 IsNonStringEnumerable { get; init; }
            public Type                 Type                  { get; init; }
            
            public static Member For(FieldInfo info) =>
                new Member {
                    Name                  = info.Name,
                    GetValue              = info.GetValue!,
                    IsNonStringEnumerable = TypeIsNonStringEnumerable(info.FieldType),
                    Type                  = info.FieldType,
                };

            public static Member For(PropertyInfo info) {
                return new Member {
                    Name                  = info.Name,
                    GetValue              = info.GetValue!,
                    IsNonStringEnumerable = TypeIsNonStringEnumerable(info.PropertyType),
                    Type                  = info.PropertyType,
                };
            }

            static bool TypeIsNonStringEnumerable(Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }
    }
}


//
// public abstract class ValueObject<T> where T : ValueObject<T> {
//     static readonly TypeMembers.Member[] Members = TypeMembers.Get(typeof(T)).ToArray();
//     
//     public override bool Equals(object other) {
//         if (ReferenceEquals(null, other))
//             return false;
//
//         if (ReferenceEquals(this, other))
//             return true;
//
//         return other.GetType() == typeof(T) 
//             && Members.All(m => m.SourceEquals(this, other));
//     }
//
//     public override int GetHashCode() =>
//         CombineHashCodes(Members.Select(m => m.GetSourceHashCode(this)));
//
//     public static bool operator ==(ValueObject<T> left, ValueObject<T> right) => Equals(left, right);
//     public static bool operator !=(ValueObject<T> left, ValueObject<T> right) => !Equals(left, right);
//     
//     // static IEnumerable<object> GetCollectionElements(object obj) {
//     //     var enumerator = ((IEnumerable)obj).GetEnumerator();
//     //     while (enumerator.MoveNext())
//     //         yield return enumerator.Current!;
//     // }
//
//     static int CombineHashCodes(IEnumerable<object> objs) {
//         var hashCode = new HashCode();
//     
//         foreach (var obj in objs)
//             hashCode.Add(obj);
//     
//         return hashCode.ToHashCode();
//     }
//     
//     static class TypeMembers {
//         const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;
//
//         public static IEnumerable<Member> Get(Type type) {
//             var baseType   = type;
//             var objectType = typeof(object);
//
//             while (baseType != objectType) {
//                 if (baseType is null)
//                     continue;
//
//                 foreach (var p in baseType.GetProperties(Flags))
//                     yield return Member.For(p);
//
//                 foreach (var f in baseType.GetFields(Flags))
//                     yield return Member.For(f);
//
//                 baseType = baseType.BaseType;
//             }
//         }
//
//         public record struct Member {
//             public string               Name                  { get; init; }
//             public Func<object, object> GetValue              { get; init; }
//             public bool                 IsNonStringEnumerable { get; init; }
//             public Type                 Type                  { get; init; }
//
//
//             //public Func<object, IEnumerable<object>> GetValues { get; init; }
//
//
//             public bool SourceEquals(object left, object right) {
//                 var thisValue  = GetValue(left);
//                 var otherValue = GetValue(right);
//
//                 return IsNonStringEnumerable
//                     ? GetCollectionElements(otherValue).SequenceEqual(GetCollectionElements(thisValue))
//                     : otherValue?.Equals(thisValue) ?? thisValue is null;
//             }
//
//             public object GetSourceHashCode(object source) {
//                 return IsNonStringEnumerable
//                     ? CombineHashCodes(GetCollectionElements(GetValue(source)))
//                     : GetValue(source);
//
//                 static int CombineHashCodes(IEnumerable<object> objs) {
//                     var hashCode = new HashCode();
//
//                     foreach (var obj in objs)
//                         hashCode.Add(obj);
//
//                     return hashCode.ToHashCode();
//                 }
//             }
//
//             public static Member For(FieldInfo info) =>
//                 new Member {
//                     Name                  = info.Name,
//                     GetValue              = info.GetValue!,
//                     IsNonStringEnumerable = TypeIsNonStringEnumerable(info.FieldType),
//                     Type                  = info.FieldType,
//                 };
//
//             public static Member For(PropertyInfo info) =>
//                 new Member {
//                     Name                  = info.Name,
//                     GetValue              = info.GetValue!,
//                     IsNonStringEnumerable = TypeIsNonStringEnumerable(info.PropertyType),
//                     Type                  = info.PropertyType,
//                 };
//
//             static bool TypeIsNonStringEnumerable(Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
//
//             static IEnumerable<object> GetCollectionElements(object obj) {
//                 var enumerator = ((IEnumerable)obj).GetEnumerator();
//                 while (enumerator.MoveNext())
//                     yield return enumerator.Current!;
//             }
//         }
//     }
// }
//
// // public static class HashCodeExtensions {
// //     public static int CombineHashCodes(this HashCode hashCode, IEnumerable<object> objs) {
// //
// //         foreach (var obj in objs)
// //             hashCode.Add(obj);
// //
// //         return hashCode.ToHashCode();
// //     }
// // }
//
// public record struct MemberA {
//     public string?              Name                   { get; init; }
//     public Func<object, object> GetValue               { get; init; }
//     public bool                 IsNonStringEnumerable  { get; init; }
//     public Type                 Type                   { get; init; }
//     public bool                 AIsNonStringEnumerable { get; init; }
//     public bool                 BIsNonStringEnumerable { get; init; }
//     public bool                 CIsNonStringEnumerable { get; init; }
//     public bool                 DIsNonStringEnumerable { get; init; }
//     public bool                 EIsNonStringEnumerable { get; init; }
//     public bool                 FIsNonStringEnumerable { get; init; }
//     public bool                 GIsNonStringEnumerable { get; init; }
//     public object?              HIsNonStringEnumerable { get; init; }
//     public object               IIsNonStringEnumerable { get; init; }
//
//     public readonly bool Equals(MemberA other) =>
//         Name == other.Name && GetValue.Equals(other.GetValue) && IsNonStringEnumerable == other.IsNonStringEnumerable && Type.Equals(other.Type)
//      && AIsNonStringEnumerable == other.AIsNonStringEnumerable && BIsNonStringEnumerable == other.BIsNonStringEnumerable
//      && CIsNonStringEnumerable == other.CIsNonStringEnumerable && DIsNonStringEnumerable == other.DIsNonStringEnumerable
//      && EIsNonStringEnumerable == other.EIsNonStringEnumerable && FIsNonStringEnumerable == other.FIsNonStringEnumerable
//      && GIsNonStringEnumerable == other.GIsNonStringEnumerable && Object.Equals(HIsNonStringEnumerable, other.HIsNonStringEnumerable)
//      && IIsNonStringEnumerable.Equals(other.IIsNonStringEnumerable);
//
//     public readonly override int GetHashCode() {
//         var hashCode = new HashCode();
//         hashCode.Add(Name);
//         hashCode.Add(GetValue);
//         hashCode.Add(IsNonStringEnumerable);
//         hashCode.Add(Type);
//         hashCode.Add(AIsNonStringEnumerable);
//         hashCode.Add(BIsNonStringEnumerable);
//         hashCode.Add(CIsNonStringEnumerable);
//         hashCode.Add(DIsNonStringEnumerable);
//         hashCode.Add(EIsNonStringEnumerable);
//         hashCode.Add(FIsNonStringEnumerable);
//         hashCode.Add(GIsNonStringEnumerable);
//         hashCode.Add(HIsNonStringEnumerable);
//         hashCode.Add(IIsNonStringEnumerable);
//         return hashCode.ToHashCode();
//     }
// }
//
// public class MemberClass : IEquatable<MemberClass> {
//     public string?              Name                   { get; init; }
//     public Func<object, object> GetValue               { get; init; }
//     public bool                 IsNonStringEnumerable  { get; init; }
//     public Type                 Type                   { get; init; }
//     public bool                 AIsNonStringEnumerable { get; init; }
//     public bool                 BIsNonStringEnumerable { get; init; }
//     public bool                 CIsNonStringEnumerable { get; init; }
//     public bool                 DIsNonStringEnumerable { get; init; }
//     public bool                 EIsNonStringEnumerable { get; init; }
//     public bool                 FIsNonStringEnumerable { get; init; }
//     public bool                 GIsNonStringEnumerable { get; init; }
//     public object?              HIsNonStringEnumerable { get; init; }
//     public object               IIsNonStringEnumerable { get; init; }
//
//     public bool Equals(MemberClass? other) {
//         if (ReferenceEquals(null, other))
//             return false;
//
//         if (ReferenceEquals(this, other))
//             return true;
//
//         return Name == other.Name && GetValue.Equals(other.GetValue) && IsNonStringEnumerable == other.IsNonStringEnumerable && Type.Equals(other.Type)
//             && AIsNonStringEnumerable == other.AIsNonStringEnumerable && BIsNonStringEnumerable == other.BIsNonStringEnumerable
//             && CIsNonStringEnumerable == other.CIsNonStringEnumerable && DIsNonStringEnumerable == other.DIsNonStringEnumerable
//             && EIsNonStringEnumerable == other.EIsNonStringEnumerable && FIsNonStringEnumerable == other.FIsNonStringEnumerable
//             && GIsNonStringEnumerable == other.GIsNonStringEnumerable && Equals(HIsNonStringEnumerable, other.HIsNonStringEnumerable)
//             && IIsNonStringEnumerable.Equals(other.IIsNonStringEnumerable);
//     }
//
//     public override bool Equals(object? obj) {
//         if (ReferenceEquals(null, obj))
//             return false;
//
//         if (ReferenceEquals(this, obj))
//             return true;
//
//         if (obj.GetType() != GetType())
//             return false;
//
//         return Equals((MemberClass)obj);
//     }
//
//     public override int GetHashCode() {
//         var hashCode = new HashCode();
//         hashCode.Add(Name);
//         hashCode.Add(GetValue);
//         hashCode.Add(IsNonStringEnumerable);
//         hashCode.Add(Type);
//         hashCode.Add(AIsNonStringEnumerable);
//         hashCode.Add(BIsNonStringEnumerable);
//         hashCode.Add(CIsNonStringEnumerable);
//         hashCode.Add(DIsNonStringEnumerable);
//         hashCode.Add(EIsNonStringEnumerable);
//         hashCode.Add(FIsNonStringEnumerable);
//         hashCode.Add(GIsNonStringEnumerable);
//         hashCode.Add(HIsNonStringEnumerable);
//         hashCode.Add(IIsNonStringEnumerable);
//         return hashCode.ToHashCode();
//     }
//
//     public static bool operator ==(MemberClass? left, MemberClass? right) => Equals(left, right);
//
//     public static bool operator !=(MemberClass? left, MemberClass? right) => !Equals(left, right);
// }
//
// // public static class TypeMembers<T> {
// //     static readonly Member[] Members = GetMembers().ToArray();
// //
// //     static IEnumerable<Member> GetMembers() {
// //         var valueObjectType = typeof(T);
// //
// //         const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
// //
// //         while (valueObjectType != typeof(object)) {
// //             if (valueObjectType is null)
// //                 continue;
// //
// //             foreach (var p in valueObjectType.GetProperties(flags))
// //                 yield return new Member(p);
// //
// //             foreach (var f in valueObjectType.GetFields(flags))
// //                 yield return new Member(f);
// //
// //             valueObjectType = valueObjectType.BaseType;
// //         }
// //     }
// //     
// //     record struct Member {
// //         public string               Name                  { get; }
// //         public Func<object, object> GetValue              { get; }
// //         public bool                 IsNonStringEnumerable { get; }
// //         public Type                 Type                  { get; }
// //
// //         public Member(MemberInfo info) {
// //             switch (info) {
// //                 case FieldInfo field:
// //                     Name                  = field.Name;
// //                     GetValue              = field.GetValue!;
// //                     IsNonStringEnumerable = TypeIsNonStringEnumerable(field.FieldType);
// //                     Type                  = field.FieldType;
// //
// //                     break;
// //
// //                 case PropertyInfo prop:
// //                     Name                  = prop.Name;
// //                     GetValue              = prop.GetValue!;
// //                     IsNonStringEnumerable = TypeIsNonStringEnumerable(prop.PropertyType);
// //                     Type                  = prop.PropertyType;
// //
// //                     break;
// //
// //                 default: throw new ArgumentException("Member is not a field or property?!", info.Name);
// //             }
// //
// //             bool TypeIsNonStringEnumerable(Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
// //         }
// //     }
// // }
//
