namespace System.Threading;

internal static class TimeoutHelper {
    public static uint GetTime() => (uint)Environment.TickCount;

    public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout) {
        var elapsedMilliseconds = GetTime() - startTime;
        if (elapsedMilliseconds > int.MaxValue) return 0;

        var currentWaitTimeout = originalWaitMillisecondsTimeout - (int)elapsedMilliseconds;
        return currentWaitTimeout <= 0 ? 0 : currentWaitTimeout;
    }
}