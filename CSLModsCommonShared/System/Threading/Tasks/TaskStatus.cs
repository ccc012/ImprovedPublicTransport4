namespace System.Threading.Tasks;

public enum TaskStatus {
    Created,
    Running,
    RanToCompletion,
    Faulted,
    Canceled
}