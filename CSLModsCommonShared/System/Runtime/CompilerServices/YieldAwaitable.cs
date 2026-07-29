using System.Threading;

namespace System.Runtime.CompilerServices;

public struct YieldAwaitable {
    public YieldAwaiter GetAwaiter() => new();

    public readonly struct YieldAwaiter : INotifyCompletion {
        public bool IsCompleted => false;

        public void OnCompleted(Action continuation) => ThreadPool.QueueUserWorkItem(_ => continuation());

        public void GetResult() { }
    }
}