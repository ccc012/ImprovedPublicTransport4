using CSLModsCommon.Utilities;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CSLModsCommon.Logging; 
public static class LogManager {
    public static string DefaultFileName { get; } = AssemblyHelper.CurrentAssemblyName;
    public static string LogDirectory { get; } = Path.Combine(Application.dataPath, "Logs");
    public static string DefaultLogPath { get; } = Path.Combine(LogDirectory, DefaultFileName + ".log");
    private static Dictionary<string, ILog> Loggers { get; set; } = new();

    static LogManager() {
        if (!Directory.Exists(LogDirectory))
            Directory.CreateDirectory(LogDirectory);
    }

    public static ILog GetLogger(string name, bool internalLogging = false) {
        if (string.IsNullOrEmpty(name))
            name = DefaultFileName;

        if (Loggers.TryGetValue(name, out var log)) return log;
        var logger = new Logger(name, LogDirectory, internalLogging);
        Loggers.Add(name, logger);
        return logger;
    }

    public static ILog GetLogger() => GetLogger(DefaultFileName);

    public static void RemoveLogger(string name) {
        if (!Loggers.TryGetValue(name, out var log)) return;
        log.Dispose();
        Loggers.Remove(name);
    }

    public static IEnumerable<ILog> GetAllLoggers() => Loggers.Values;

    public static void ClearAllLoggers() {
        foreach (var logger in Loggers.Values) logger.Dispose();

        Loggers.Clear();
    }
}