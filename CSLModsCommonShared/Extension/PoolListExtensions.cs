using ColossalFramework;
using System;

namespace CSLModsCommon.Extension;

public static class PoolListExtensions {
    public static void MoveToFront<T>(this PoolList<T> list, T item) {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        var index = list.IndexOf(item);
        if (index <= 0) return;
        list.RemoveAt(index);
        list.Insert(0, item);
    }

    public static void MoveToBack<T>(this PoolList<T> list, T item) {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        var index = list.IndexOf(item);
        if (index == -1 || index == list.Count - 1) return;
        list.RemoveAt(index);
        list.Add(item);
    }
}