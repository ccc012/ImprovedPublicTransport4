using System;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.Extension; 
public static class LinqExtensions {
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> source) => source == null || !source.Any();

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        foreach (var item in source)
            if (item != null)
                yield return item;
    }

    public static bool None<T>(this IEnumerable<T> source) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return !source.Any();
    }

    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        return !source.Any(predicate);
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        foreach (var item in source)
            action(item);
    }

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
            if (seenKeys.Add(keySelector(element)))
                yield return element;
    }

    public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        using var e = source.GetEnumerator();
        if (!e.MoveNext())
            throw new InvalidOperationException("Sequence contains no elements.");

        var minElem = e.Current;
        var minKey = selector(minElem);

        while (e.MoveNext()) {
            var current = e.Current;
            var currentKey = selector(current);
            if (currentKey.CompareTo(minKey) >= 0) continue;
            minElem = current;
            minKey = currentKey;
        }

        return minElem;
    }

    public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey> {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        using var e = source.GetEnumerator();
        if (!e.MoveNext())
            throw new InvalidOperationException("Sequence contains no elements.");

        var maxElem = e.Current;
        var maxKey = selector(maxElem);

        while (e.MoveNext()) {
            var current = e.Current;
            var currentKey = selector(current);
            if (currentKey.CompareTo(maxKey) <= 0) continue;
            maxElem = current;
            maxKey = currentKey;
        }

        return maxElem;
    }
}