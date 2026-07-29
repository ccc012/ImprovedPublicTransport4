using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

public struct AsyncTaskMethodBuilder {
    private TaskCompletionSource<object> _tcs;

    public static AsyncTaskMethodBuilder Create() => new() {
        _tcs = new TaskCompletionSource<object>()
    };

    public Task Task => _tcs.Task;

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    public void SetResult() => _tcs.SetResult(null);

    public void SetException(Exception exception) => _tcs.SetException(exception);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine => awaiter.OnCompleted(stateMachine.MoveNext);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine => awaiter.OnCompleted(stateMachine.MoveNext);
}

public struct AsyncTaskMethodBuilder<TResult> {
    private TaskCompletionSource<TResult> _tcs;

    public static AsyncTaskMethodBuilder<TResult> Create() => new() {
        _tcs = new TaskCompletionSource<TResult>()
    };

    public Task<TResult> Task => _tcs.Task;

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    public void SetResult(TResult result) => _tcs.SetResult(result);

    public void SetException(Exception exception) => _tcs.SetException(exception);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine => awaiter.OnCompleted(stateMachine.MoveNext);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine => awaiter.OnCompleted(stateMachine.MoveNext);
}