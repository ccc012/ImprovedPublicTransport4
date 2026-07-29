using System.Collections.Generic;

namespace CSLModsCommon.Collections;

public interface IReadOnlyDictionary<TKey, TValue> :
    IReadOnlyCollection<KeyValuePair<TKey, TValue>> {
    IEnumerable<TKey> Keys { get; }
    IEnumerable<TValue> Values { get; }

    bool ContainsKey(TKey key);
    bool TryGetValue(TKey key, out TValue value);

    TValue this[TKey key] { get; }
}