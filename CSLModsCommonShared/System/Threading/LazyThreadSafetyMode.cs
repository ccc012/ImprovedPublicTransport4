namespace System.Threading;

public enum LazyThreadSafetyMode {
    None = 0,
    PublicationOnly = 1,
    ExecutionAndPublication = 2
}