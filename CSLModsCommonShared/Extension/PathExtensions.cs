using System;
using System.IO;

namespace CSLModsCommon.Extension;

public static class PathExtensions {
    public static string Combine(string path1, string path2) => Path.Combine(path1, path2);

    public static string Combine(string path1, string path2, string path3) => Path.Combine(Path.Combine(path1, path2), path3);

    public static string Combine(string path1, string path2, string path3, string path4) => Path.Combine(Path.Combine(Path.Combine(path1, path2), path3), path4);

    public static string Combine(params string[] paths) {
        if (paths == null || paths.Length == 0)
            throw new ArgumentException("Paths can't be null or empty", nameof(paths));
        var combinedPath = paths[0];
        for (var i = 1; i < paths.Length; i++) combinedPath = Path.Combine(combinedPath, paths[i]);

        return combinedPath;
    }
}