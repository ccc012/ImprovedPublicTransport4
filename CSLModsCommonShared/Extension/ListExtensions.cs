using System.Collections.Generic;

namespace CSLModsCommon.Extension; 
public static class ListExtensions {
    public static T Last<T>(this List<T> list) => list[list.Count - 1];

    public static T PopLast<T>(this List<T> list) {
        var value = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
        return value;
    }
}