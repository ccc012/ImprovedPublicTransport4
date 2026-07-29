using System;
using System.Diagnostics;

namespace CSLModsCommon.Utilities;

public class PerformanceCounter : IDisposable {
    private readonly Stopwatch _stopwatch = new();
    private readonly Action<TimeSpan> _resultCallback;
    private readonly Action<PerformanceCounter> _counterCallback;

    public TimeSpan Result => _stopwatch.Elapsed;
    public string ReportSeconds => $"{Result.TotalSeconds:F3}s";
    public string ReportMilliseconds => $"{Result.TotalMilliseconds:F3}ms";

    public PerformanceCounter() => _stopwatch.Start();

    public PerformanceCounter(Action<TimeSpan> callback) : this() => _resultCallback = callback;

    public PerformanceCounter(Action<PerformanceCounter> callback) : this() => _counterCallback = callback;

    public static PerformanceCounter Start(Action<TimeSpan> callback) => new PerformanceCounter(callback);

    public static PerformanceCounter Start(Action<PerformanceCounter> callback) => new PerformanceCounter(callback);

    public void Report(Action<TimeSpan> callback) => callback(Result);

    public void Dispose() {
        _stopwatch.Stop();
        _resultCallback?.Invoke(Result);
        _counterCallback?.Invoke(this);
    }
}