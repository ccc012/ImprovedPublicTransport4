using System;
using System.Collections.Generic;

namespace CSLModsCommon.Extension; 
public static class ListExtensions {
    public static T Last<T>(this List<T> list) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (list.Count == 0) throw new InvalidOperationException("List is empty.");
        return list[list.Count - 1];
    }

    public static T PopLast<T>(this List<T> list) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (list.Count == 0) throw new InvalidOperationException("List is empty.");
        var value = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
        return value;
    }
}
