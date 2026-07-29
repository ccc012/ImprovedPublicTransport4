using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

public class Task {
    private readonly List<Action> _continuations = new();
    private readonly object _lock = new();

    public bool IsCompleted { get; private set; }
    public bool IsFaulted { get; private set; }
    public bool IsCanceled { get; private set; }
    public AggregateException Exception { get; private set; }

    public TaskStatus Status {
        get {
            if (IsCanceled) return TaskStatus.Canceled;
            if (IsFaulted) return TaskStatus.Faulted;
            if (IsCompleted) return TaskStatus.RanToCompletion;
            return TaskStatus.Running;
        }
    }

    public static YieldAwaitable Yield() => new();

    public static Task Delay(int milliseconds) {
        var tcs = new TaskCompletionSource<object>();

        Timer timer = null;
        timer = new Timer(_ => {
            timer.Dispose();
            tcs.SetResult(null);
        }, null, milliseconds, Timeout.Infinite);

        return tcs.Task;
    }

    public static Task FromException(Exception ex) {
        var tcs = new TaskCompletionSource<object>();
        tcs.SetException(ex);
        return tcs.Task;
    }

    public static Task<TResult> FromException<TResult>(Exception ex) {
        var tcs = new TaskCompletionSource<TResult>();
        tcs.SetException(ex);
        return tcs.Task;
    }

    public static Task FromCanceled() {
        var tcs = new TaskCompletionSource<object>();
        tcs.SetCanceled();
        return tcs.Task;
    }

    public static Task Run(Action action) {
        var tcs = new TaskCompletionSource<object>();
        ThreadPool.QueueUserWorkItem(_ => {
            try {
                action();
                tcs.SetResult(null);
            }
            catch (Exception ex) {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    public static Task<TResult> FromResult<TResult>(TResult result) {
        var tcs = new TaskCompletionSource<TResult>();
        tcs.SetResult(result);
        return tcs.Task;
    }

    public static Task<Task> WhenAny(params Task[] tasks) {
        if (tasks == null || tasks.Length == 0)
            throw new ArgumentException("No tasks provided.");

        var tcs = new TaskCompletionSource<Task>();

        foreach (var task in tasks) {
            task.ContinueWith(t => { tcs.TrySetResult(t); });
        }

        return tcs.Task;
    }

    public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) {
        var tcs = new TaskCompletionSource<TResult[]>();

        if (tasks == null)
            throw new ArgumentNullException(nameof(tasks));

        if (tasks.Length == 0) {
            tcs.SetResult(new TResult[0]);
            return tcs.Task;
        }

        var remaining = tasks.Length;
        var results = new TResult[tasks.Length];

        for (var i = 0; i < tasks.Length; i++) {
            var index = i;
            tasks[i]
                .ContinueWith(t => {
                    if (t.IsFaulted) {
                        tcs.TrySetException(t.Exception);
                        return;
                    }

                    results[index] = t.Result;

                    if (Interlocked.Decrement(ref remaining) == 0)
                        tcs.SetResult(results);
                });
        }

        return tcs.Task;
    }

    public static Task WhenAll(params Task[] tasks) {
        var tcs = new TaskCompletionSource<object>();
        var remaining = tasks.Length;

        if (remaining == 0) {
            tcs.SetResult(null);
            return tcs.Task;
        }

        foreach (var task in tasks) {
            task.ContinueWith(t => {
                if (t.IsFaulted) {
                    tcs.SetException(t.Exception);
                    return;
                }

                if (Interlocked.Decrement(ref remaining) == 0)
                    tcs.SetResult(null);
            });
        }

        return tcs.Task;
    }

    public TaskAwaiter GetAwaiter() => new(this);

    public Task ContinueWith(Action<Task> continuation) {
        var tcs = new TaskCompletionSource<object>();

        var wrapper = () => {
            try {
                continuation(this);
                tcs.SetResult(null);
            }
            catch (Exception ex) {
                tcs.SetException(ex);
            }
        };

        var runNow = false;

        lock (_lock) {
            if (IsCompleted || IsFaulted || IsCanceled)
                runNow = true;
            else
                _continuations.Add(wrapper);
        }

        if (runNow)
            wrapper();

        return tcs.Task;
    }

    public void Wait() {
        lock (_lock) {
            while (!IsCompleted && !IsFaulted && !IsCanceled)
                Monitor.Wait(_lock);
        }

        if (IsCanceled)
            throw new TaskCanceledException();

        if (IsFaulted)
            throw Exception;
    }

    internal bool TrySetCanceled() {
        lock (_lock) {
            if (IsCompleted || IsFaulted || IsCanceled)
                return false;

            IsCanceled = true;
            Monitor.PulseAll(_lock);
        }

        RunContinuations();
        return true;
    }

    internal void AddContinuation(Action continuation) {
        var runNow = false;

        lock (_lock) {
            if (IsCompleted || IsFaulted || IsCanceled)
                runNow = true;
            else
                _continuations.Add(continuation);
        }

        if (runNow)
            continuation();
    }

    internal bool TrySetResult() {
        lock (_lock) {
            if (IsCompleted || IsFaulted || IsCanceled)
                return false;

            IsCompleted = true;
            Monitor.PulseAll(_lock);
        }

        RunContinuations();
        return true;
    }

    internal bool TrySetException(Exception ex) {
        lock (_lock) {
            if (IsCompleted || IsFaulted || IsCanceled)
                return false;

            IsFaulted = true;
            Exception = new AggregateException(ex);
            Monitor.PulseAll(_lock);
        }

        RunContinuations();
        return true;
    }

    private void RunContinuations() {
        Action[] list;

        lock (_lock) {
            list = _continuations.ToArray();
            _continuations.Clear();
        }

        foreach (var c in list)
            c();
    }
}

public class Task<TResult> : Task {
    private TResult _result;

    public TResult Result {
        get {
            Wait();
            return IsFaulted ? throw Exception : _result;
        }
    }

    public static Task<TResult> Run(Func<TResult> function) {
        if (function == null)
            throw new ArgumentNullException(nameof(function));

        var tcs = new TaskCompletionSource<TResult>();
        ThreadPool.QueueUserWorkItem(_ => {
            try {
                var result = function();
                tcs.SetResult(result);
            }
            catch (Exception ex) {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public new TaskAwaiter<TResult> GetAwaiter() => new(this);

    public Task ContinueWith(Action<Task<TResult>> continuation) => base.ContinueWith(t => continuation((Task<TResult>)t));

    internal bool TrySetResult(TResult result) {
        _result = result;
        return base.TrySetResult();
    }
}