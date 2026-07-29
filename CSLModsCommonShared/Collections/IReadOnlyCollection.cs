using System.Collections.Generic;

namespace CSLModsCommon.Collections;

public interface IReadOnlyCollection<T> : IEnumerable<T> {
    int Count { get; }
}