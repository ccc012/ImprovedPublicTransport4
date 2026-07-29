using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSLModsCommon.Utilities; 
public static class DirectoryHelper {
    public static void EnsureDirectoryExists(string directoryPath) {
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
    }

    public static bool SafeDeleteDirectory(string directoryPath, bool recursive = true) {
        try {
            if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath, recursive);

            return true;
        }
        catch {
            return false;
        }
    }

    public static void ClearDirectory(string directoryPath) {
        if (!Directory.Exists(directoryPath)) return;

        foreach (var file in Directory.GetFiles(directoryPath)) {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (var dir in Directory.GetDirectories(directoryPath)) SafeDeleteDirectory(dir);
    }

    public static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true) {
        if (!Directory.Exists(sourceDir)) throw new DirectoryNotFoundException($"Source directory is not exist: {sourceDir}");

        EnsureDirectoryExists(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir)) {
            var desFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, desFile, overwrite);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir)) {
            var desDir = Path.Combine(destinationDir, Path.GetFileName(dir));
            CopyDirectory(dir, desDir, overwrite);
        }
    }

    public static long GetDirectorySize(string directoryPath) {
        if (!Directory.Exists(directoryPath)) return 0;

        return Directory.GetFiles(directoryPath).Sum(file => new FileInfo(file).Length) + Directory.GetDirectories(directoryPath).Sum(dir => GetDirectorySize(dir));
    }

    public static List<string> GetAllFiles(string directoryPath, string searchPattern = "*", bool recursive = true) {
        if (!Directory.Exists(directoryPath)) return new List<string>();

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(directoryPath, searchPattern, option).ToList();
    }

    public static List<string> GetAllDirectories(string directoryPath, bool recursive = true) {
        if (!Directory.Exists(directoryPath)) return new List<string>();

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetDirectories(directoryPath, "*", option).ToList();
    }

    public static bool RenameDirectory(string sourceDir, string newName) {
        try {
            if (!Directory.Exists(sourceDir)) return false;

            var parentDir = Path.GetDirectoryName(sourceDir);
            if (parentDir is null) return false;
            var desDir = Path.Combine(parentDir, newName);

            if (Directory.Exists(desDir)) return false;

            Directory.Move(sourceDir, desDir);
            return true;
        }
        catch {
            return false;
        }
    }

    public static bool MoveDirectory(string sourceDir, string desDir) {
        try {
            if (!Directory.Exists(sourceDir)) return false;

            if (Directory.Exists(desDir)) return false;

            Directory.Move(sourceDir, desDir);
            return true;
        }
        catch {
            return false;
        }
    }

    public static DateTime GetLastWriteTime(string directoryPath) {
        if (!Directory.Exists(directoryPath)) throw new DirectoryNotFoundException($"Source directory is not exist: {directoryPath}");

        return Directory.GetLastWriteTime(directoryPath);
    }

    public static bool CompareDirectories(string dir1, string dir2, bool compareFileContents = false) {
        if (!Directory.Exists(dir1) || !Directory.Exists(dir2)) return false;

        var files1 = GetAllFiles(dir1);
        var files2 = GetAllFiles(dir2);
        var dirs1 = GetAllDirectories(dir1);
        var dirs2 = GetAllDirectories(dir2);

        if (files1.Count != files2.Count || dirs1.Count != dirs2.Count) return false;

        for (var i = 0; i < files1.Count; i++) {
            var relativePath1 = files1[i].Substring(dir1.Length).Trim(Path.DirectorySeparatorChar);
            var relativePath2 = files2[i].Substring(dir2.Length).Trim(Path.DirectorySeparatorChar);

            if (!relativePath1.Equals(relativePath2, StringComparison.OrdinalIgnoreCase)) return false;

            if (compareFileContents) {
                if (!FileCompare(files1[i], files2[i])) return false;
            }
            else {
                var fi1 = new FileInfo(files1[i]);
                var fi2 = new FileInfo(files2[i]);
                if (fi1.Length != fi2.Length) return false;
            }
        }

        return true;
    }

    private static bool FileCompare(string file1, string file2) {
        using var fs1 = new FileStream(file1, FileMode.Open);
        using var fs2 = new FileStream(file2, FileMode.Open);
        if (fs1.Length != fs2.Length) return false;

        int byte1, byte2;
        do {
            byte1 = fs1.ReadByte();
            byte2 = fs2.ReadByte();
            if (byte1 != byte2) return false;
        } while (byte1 != -1 && byte2 != -1);

        return true;
    }
}