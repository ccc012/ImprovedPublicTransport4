using System;
using System.Diagnostics;

namespace CSLModsCommon.Extension;

public static class StopwatchExtensions {
    public static void Restart(this Stopwatch sw) {
        sw.Reset();
        sw.Start();
    }

    public static string GetTotalMillisecondsString(this Stopwatch sw) => sw.Elapsed.TotalMilliseconds.ToString("F3") + " ms";

    public static string FormatElapsed(this Stopwatch sw) {
        var ts = sw.Elapsed;
        return $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }

    public static long TimeAction(Action action) {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    public static long TimeFunc<T>(Func<T> func, out T result) {
        var sw = Stopwatch.StartNew();
        result = func();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}