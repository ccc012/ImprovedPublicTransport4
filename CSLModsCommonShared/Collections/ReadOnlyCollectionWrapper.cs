using System;
using System.Collections;
using System.Collections.Generic;

namespace CSLModsCommon.Collections;

public class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T> {
    private readonly ICollection<T> _collection;

    public int Count => _collection.Count;

    public ReadOnlyCollectionWrapper(ICollection<T> collection) => _collection = collection ?? throw new ArgumentNullException(nameof(collection));

    public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}