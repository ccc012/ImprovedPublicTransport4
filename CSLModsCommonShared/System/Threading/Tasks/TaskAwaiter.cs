using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

public readonly struct TaskAwaiter : INotifyCompletion {
    private readonly Task _task;

    public bool IsCompleted => _task.IsCompleted || _task.IsFaulted || _task.IsCanceled;

    public TaskAwaiter(Task task) => _task = task;

    public void OnCompleted(Action continuation) => _task.AddContinuation(continuation);

    public void GetResult() {
        if (_task.IsFaulted)
            throw _task.Exception;

        if (_task.IsCanceled)
            throw new TaskCanceledException();
    }
}

public readonly struct TaskAwaiter<TResult> : INotifyCompletion {
    private readonly Task<TResult> _task;

    public bool IsCompleted => _task.IsCompleted || _task.IsFaulted || _task.IsCanceled;

    public TaskAwaiter(Task<TResult> task) => _task = task;

    public void OnCompleted(Action continuation) => _task.AddContinuation(continuation);

    public TResult GetResult() {
        if (_task.IsFaulted)
            throw _task.Exception;

        if (_task.IsCanceled)
            throw new TaskCanceledException();

        return _task.Result;
    }
}