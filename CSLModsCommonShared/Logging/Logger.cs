using ColossalFramework.UI;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Dialogs;
using CSLModsCommon.Utilities;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace CSLModsCommon.Logging; 
public class Logger : ILog {
    private readonly object _fileLock = new();
    private readonly StreamWriter _writer;

    public string Name { get; set; }
    public bool InternalLogging { get; set; }
    public string LogPath { get; }
    public LogLevel Level { get; set; } = LogLevel.Info;
    public bool IsVerboseEnabled => Level <= LogLevel.Verbose;
    public bool IsDebugEnabled => Level <= LogLevel.Debug;
    public bool IsInfoEnabled => Level <= LogLevel.Info;
    public bool IsWarningEnabled => Level <= LogLevel.Warn;
    public bool IsErrorEnabled => Level <= LogLevel.Error;
    public bool IsFatalEnabled => Level <= LogLevel.Fatal;
    public bool IsLoggingEnabled => Level != LogLevel.Disabled;
    public bool ShowsErrorsInUI { get; private set; }

    public Logger(string name, string logDirectory, bool internalLogging = false) {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        Name = name;
        LogPath = Path.Combine(logDirectory, Name + ".log");
        InternalLogging = internalLogging;

        if (File.Exists(LogPath))
            File.WriteAllText(LogPath, string.Empty);

        _writer = new StreamWriter(LogPath, true, Encoding.UTF8);
        _writer.WriteLine($"{GetTag(LogLevel.Info)} {GetEnvironmentInfo()}");
        _writer.Flush();
    }

    public ILog SetLevel(LogLevel level) {
        Level = level;
        LogManager.GetLogger().Verbose($"Set logger {Name} level to {level}");
        return this;
    }

    public ILog SetInternalLogging(bool internalLogging) {
        InternalLogging = internalLogging;
        LogManager.GetLogger().Verbose($"Set internal log: {internalLogging}");
        return this;
    }

    public ILog SetShowsErrorsInUI() {
        ShowsErrorsInUI = true;
        LogManager.GetLogger().Verbose($"Set shows errors in UI: {ShowsErrorsInUI}");
        return this;
    }

    public void Verbose(Exception exception) => Log(LogLevel.Verbose, exception: exception);

    public void Verbose(string message) => Log(LogLevel.Verbose, message);

    public void Verbose(object message) => Log(LogLevel.Verbose, message?.ToString());

    public void Verbose(Exception exception, object message) => Log(LogLevel.Verbose, message?.ToString(), exception);

    public void VerboseFormat(string format, object p) => Log(LogLevel.Verbose, string.Format(format, p));

    public void VerboseFormat(string format, object p1, object p2) => Log(LogLevel.Verbose, string.Format(format, p1, p2));

    public void VerboseFormat(string format, object p1, object p2, object p3) => Log(LogLevel.Verbose, string.Format(format, p1, p2, p3));

    public void VerboseFormat(string format, params object[] p) => Log(LogLevel.Verbose, string.Format(format, p));

    public void Debug(Exception exception) => Log(LogLevel.Debug, exception: exception);

    public void Debug(string message) => Log(LogLevel.Debug, message);

    public void Debug(object message) => Log(LogLevel.Debug, message?.ToString());

    public void Debug(Exception exception, object message) => Log(LogLevel.Debug, message?.ToString(), exception);

    public void DebugFormat(string format, object p) => Log(LogLevel.Debug, string.Format(format, p));

    public void DebugFormat(string format, object p1, object p2) => Log(LogLevel.Debug, string.Format(format, p1, p2));

    public void DebugFormat(string format, object p1, object p2, object p3) => Log(LogLevel.Debug, string.Format(format, p1, p2, p3));

    public void DebugFormat(string format, params object[] p) => Log(LogLevel.Debug, string.Format(format, p));

    public void Info(Exception exception) => Log(LogLevel.Info, exception: exception);

    public void Info(string message) => Log(LogLevel.Info, message);

    public void Info(object message) => Log(LogLevel.Info, message?.ToString());

    public void Info(Exception exception, object message) => Log(LogLevel.Info, message?.ToString(), exception);

    public void InfoFormat(string format, object p) => Log(LogLevel.Info, string.Format(format, p));

    public void InfoFormat(string format, object p1, object p2) => Log(LogLevel.Info, string.Format(format, p1, p2));

    public void InfoFormat(string format, params object[] p) => Log(LogLevel.Info, string.Format(format, p));

    public void Warn(Exception exception) => Log(LogLevel.Warn, exception: exception);

    public void Warn(string message) => Log(LogLevel.Warn, message);

    public void Warn(object message) => Log(LogLevel.Warn, message?.ToString());

    public void Warn(Exception exception, object message) => Log(LogLevel.Warn, message?.ToString(), exception);

    public void WarnFormat(string format, object p) => Log(LogLevel.Warn, string.Format(format, p));

    public void WarnFormat(string format, object p1, object p2) => Log(LogLevel.Warn, string.Format(format, p1, p2));

    public void WarnFormat(string format, params object[] p) => Log(LogLevel.Warn, string.Format(format, p));

    public void Error(Exception exception) => Log(LogLevel.Error, exception: exception);

    public void Error(string message) => Log(LogLevel.Error, message);

    public void Error(object message) => Log(LogLevel.Error, message?.ToString());

    public void Error(Exception exception, object message) => Log(LogLevel.Error, message?.ToString(), exception);

    public void ErrorFormat(string format, object p) => Log(LogLevel.Error, string.Format(format, p));

    public void ErrorFormat(string format, object p1, object p2) => Log(LogLevel.Error, string.Format(format, p1, p2));

    public void ErrorFormat(string format, params object[] p) => Log(LogLevel.Error, string.Format(format, p));

    public void Fatal(Exception exception) => Log(LogLevel.Fatal, exception: exception);

    public void Fatal(string message) => Log(LogLevel.Fatal, message);

    public void Fatal(object message) => Log(LogLevel.Fatal, message?.ToString());

    public void Fatal(Exception exception, object message) => Log(LogLevel.Fatal, message?.ToString(), exception);

    public void FatalFormat(string format, object p) => Log(LogLevel.Fatal, string.Format(format, p));

    public void FatalFormat(string format, object p1, object p2) => Log(LogLevel.Fatal, string.Format(format, p1, p2));

    public void FatalFormat(string format, params object[] p) => Log(LogLevel.Fatal, string.Format(format, p));

    public void Log(LogLevel logLevel, string message = "", Exception exception = null) {
        if (!InternalLogging) {
            if (Level == LogLevel.Disabled || logLevel < Level) return;
            lock (_fileLock) {
                var e = exception is null ? string.Empty : $"\n{exception.Message}\n{exception.StackTrace}";
                _writer.WriteLine($"{GetTag(logLevel)} {message} {e}");
                _writer.Flush();
                if (!ShowsErrorsInUI || UIView.GetAView() is null) return;

                if (logLevel is LogLevel.Error or LogLevel.Fatal)
                    Domain.DefaultDomain.GetOrCreateManager<DialogManager>().Show<ErrorExceptionDialog>().AddContent(message, $"{exception?.Message}\n {exception?.StackTrace}");
                else if (logLevel is LogLevel.Warn) Domain.DefaultDomain.GetOrCreateManager<DialogManager>().Show<WarningExceptionDialog>().AddContent(message, $"{exception?.Message}\n {exception?.StackTrace}");
            }
        }
        else {
            UnityEngine.Debug.logger.Log($"[{AssemblyHelper.CurrentAssemblyName}] {GetTag(logLevel)} {message} {exception?.Message}");
        }
    }

    public void Dispose() => _writer?.Dispose();

    public string GetEnvironmentInfo() {
        var sb = new StringBuilder();
        sb.AppendLine("Environment information:");
        sb.AppendLine($"Operating system: {SystemInfo.operatingSystem}");
        sb.AppendLine($"System language: {Application.systemLanguage}");
        sb.AppendLine($"Unity version: {Application.unityVersion}");
        sb.AppendLine($"Game version: {BuildConfig.applicationVersion}");
        sb.AppendLine($"Mod version: {AssemblyHelper.CurrentAssemblyVersion}");
        sb.AppendLine($".NET runtime: {Environment.Version}");
        return sb.ToString();
    }

    private string GetTag(LogLevel logLevel) => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel.ToString().ToUpper()}]";
}