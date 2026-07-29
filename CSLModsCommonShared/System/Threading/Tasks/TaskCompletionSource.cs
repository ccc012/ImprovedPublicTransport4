namespace System.Threading.Tasks;

public class TaskCompletionSource<TResult> {
    public Task<TResult> Task { get; } = new();

    public bool TrySetResult(TResult result) => Task.TrySetResult(result);

    public bool TrySetException(Exception ex) => Task.TrySetException(ex);

    public void SetResult(TResult result) {
        if (!TrySetResult(result))
            throw new InvalidOperationException("Task already completed.");
    }

    public void SetException(Exception ex) {
        if (!TrySetException(ex))
            throw new InvalidOperationException("Task already completed.");
    }

    public bool TrySetCanceled() => Task.TrySetCanceled();

    public void SetCanceled() {
        if (!TrySetCanceled())
            throw new InvalidOperationException("Task already completed.");
    }
}