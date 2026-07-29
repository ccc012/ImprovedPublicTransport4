using CSLModsCommon.Collections;
using System.Collections.Generic;

namespace CSLModsCommon.Extension; 
public static class ReadOnlyExtensions {
    public static CSLModsCommon.Collections.IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dict) => new ReadOnlyDictionaryWrapper<TKey, TValue>(dict);

    public static CSLModsCommon.Collections.IReadOnlyList<T> AsReadOnly<T>(this IList<T> list) => new ReadOnlyListWrapper<T>(list);

    public static CSLModsCommon.Collections.IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> collection) => new ReadOnlyCollectionWrapper<T>(collection);
}