namespace CSLModsCommon.Collections;

public interface IReadOnlyList<T> : IReadOnlyCollection<T> {
    T this[int index] { get; }
}