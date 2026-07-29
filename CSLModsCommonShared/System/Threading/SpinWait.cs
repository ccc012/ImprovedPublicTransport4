namespace System.Threading;

public struct SpinWait {
    internal const int YieldThreshold = 10;
    internal const int Sleep0EveryHowManyTimes = 5;
    internal const int Sleep1EveryHowManyTimes = 20;

    public int Count { get; private set; }
    public bool NextSpinWillYield => Count > YieldThreshold || PlatformHelper.IsSingleProcessor;

    public void SpinOnce() {
        if (NextSpinWillYield) {
            var yieldsSoFar = Count >= YieldThreshold ? Count - YieldThreshold : Count;

            if (yieldsSoFar % Sleep1EveryHowManyTimes == Sleep1EveryHowManyTimes - 1)
                Thread.Sleep(1);
            else if (yieldsSoFar % Sleep0EveryHowManyTimes == Sleep0EveryHowManyTimes - 1)
                Thread.Sleep(0);
            else
                Thread.SpinWait(4 << YieldThreshold);
        }
        else {
            Thread.SpinWait(4 << Count);
        }

        Count = Count == int.MaxValue ? YieldThreshold : Count + 1;
    }

    public void Reset() => Count = 0;

    public static void SpinUntil(Func<bool> condition) => SpinUntil(condition, Timeout.Infinite);

    public static bool SpinUntil(Func<bool> condition, TimeSpan timeout) {
        var totalMilliseconds = (long)timeout.TotalMilliseconds;
        if (totalMilliseconds is < -1 or > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(timeout), "Timeout Wrong");

        return SpinUntil(condition, (int)timeout.TotalMilliseconds);
    }

    public static bool SpinUntil(Func<bool> condition, int millisecondsTimeout) {
        if (millisecondsTimeout < Timeout.Infinite)
            throw new ArgumentOutOfRangeException(
                nameof(millisecondsTimeout), "Milliseconds Timeout");

        if (condition == null) throw new ArgumentNullException(nameof(condition));

        uint startTime = 0;
        if (millisecondsTimeout != 0 && millisecondsTimeout != Timeout.Infinite) startTime = TimeoutHelper.GetTime();

        var spinner = new SpinWait();
        while (!condition()) {
            if (millisecondsTimeout == 0) return false;

            spinner.SpinOnce();
            if (millisecondsTimeout == Timeout.Infinite || !spinner.NextSpinWillYield) continue;

            if (millisecondsTimeout <= TimeoutHelper.GetTime() - startTime) return false;
        }

        return true;
    }
}