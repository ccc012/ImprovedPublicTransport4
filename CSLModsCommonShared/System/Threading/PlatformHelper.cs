using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

internal static class PlatformHelper {
    private const int ProcessorCountRefreshIntervalMS = 30000;
    private static volatile int _sProcessorCount;
    private static volatile int _sLastProcessorCountRefreshTicks;

    [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
    internal static int ProcessorCount {
        get {
            var now = Environment.TickCount;
            var procCount = _sProcessorCount;

            if (procCount != 0 && now - _sLastProcessorCountRefreshTicks < ProcessorCountRefreshIntervalMS) return procCount;
            _sProcessorCount = procCount = Environment.ProcessorCount;
            _sLastProcessorCountRefreshTicks = now;

            return procCount;
        }
    }

    internal static bool IsSingleProcessor => ProcessorCount == 1;
}