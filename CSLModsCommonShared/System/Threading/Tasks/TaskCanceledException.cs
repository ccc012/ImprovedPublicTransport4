namespace System.Threading.Tasks;

public class TaskCanceledException : OperationCanceledException {
    public Task Task { get; private set; }
        
    public TaskCanceledException() { }

    public TaskCanceledException(string message) : base(message) { }

    public TaskCanceledException(string message, Exception innerException) : base(message, innerException) { }

    public TaskCanceledException(Task task) : base("A task was canceled.") => Task = task;
}