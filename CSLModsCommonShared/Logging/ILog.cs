using System;

namespace CSLModsCommon.Logging;

public interface ILog : IDisposable {
    string Name { get; }
    bool InternalLogging { get; set; }
    string LogPath { get; }
    LogLevel Level { get; set; }
    bool IsVerboseEnabled { get; }
    bool IsDebugEnabled { get; }
    bool IsInfoEnabled { get; }
    bool IsWarningEnabled { get; }
    bool IsErrorEnabled { get; }
    bool IsFatalEnabled { get; }
    bool IsLoggingEnabled { get; }
    bool ShowsErrorsInUI { get; }

    ILog SetLevel(LogLevel level);
    ILog SetInternalLogging(bool internalLogging);
    ILog SetShowsErrorsInUI();
    void Log(LogLevel logLevel, string message, Exception exception);
    void Verbose(Exception exception);
    void Verbose(string message);
    void Verbose(object message);
    void Verbose(Exception exception, object message);
    void VerboseFormat(string format, object p);
    void VerboseFormat(string format, object p1, object p2);
    void VerboseFormat(string format, object p1, object p2, object p3);
    void VerboseFormat(string format, params object[] p);
    void Debug(Exception exception);
    void Debug(string message);
    void Debug(object message);
    void Debug(Exception exception, object message);
    void DebugFormat(string format, object p);
    void DebugFormat(string format, object p1, object p2);
    void DebugFormat(string format, object p1, object p2, object p3);
    void DebugFormat(string format, params object[] p);
    void Info(Exception exception);
    void Info(string message);
    void Info(object message);
    void Info(Exception exception, object message);
    void InfoFormat(string format, object p);
    void InfoFormat(string format, object p1, object p2);
    void InfoFormat(string format, params object[] p);
    void Warn(Exception exception);
    void Warn(string message);
    void Warn(object message);
    void Warn(Exception exception, object message);
    void WarnFormat(string format, object p);
    void WarnFormat(string format, object p1, object p2);
    void WarnFormat(string format, params object[] p);
    void Error(Exception exception);
    void Error(string message);
    void Error(object message);
    void Error(Exception exception, object message);
    void ErrorFormat(string format, object p);
    void ErrorFormat(string format, object p1, object p2);
    void ErrorFormat(string format, params object[] p);
    void Fatal(Exception exception);
    void Fatal(string message);
    void Fatal(object message);
    void Fatal(Exception exception, object message);
    void FatalFormat(string format, object p);
    void FatalFormat(string format, object p1, object p2);
    void FatalFormat(string format, params object[] p);
}