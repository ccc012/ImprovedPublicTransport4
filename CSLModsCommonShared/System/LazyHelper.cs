using System.Threading;

namespace System;

internal class LazyHelper {
    internal static readonly LazyHelper NoneViaConstructor = new(LazyState.NoneViaConstructor);
    internal static readonly LazyHelper NoneViaFactory = new(LazyState.NoneViaFactory);
    internal static readonly LazyHelper PublicationOnlyViaConstructor = new(LazyState.PublicationOnlyViaConstructor);
    internal static readonly LazyHelper PublicationOnlyViaFactory = new(LazyState.PublicationOnlyViaFactory);
    internal static readonly LazyHelper PublicationOnlyWaitForOtherThreadToPublish = new(LazyState.PublicationOnlyWait);

    private readonly Exception _exception;

    internal LazyState State { get; }

    internal LazyHelper(LazyState state) => State = state;

    internal LazyHelper(LazyThreadSafetyMode mode, Exception exception) {
        State = mode switch {
            LazyThreadSafetyMode.None => LazyState.NoneException,
            LazyThreadSafetyMode.PublicationOnly => LazyState.PublicationOnlyException,
            LazyThreadSafetyMode.ExecutionAndPublication => LazyState.ExecutionAndPublicationException,
            _ => State
        };

        _exception = exception;
    }

    internal static LazyThreadSafetyMode? GetMode(LazyHelper state) => state?.GetMode();

    internal static bool GetIsValueFaulted(LazyHelper state) => state is { _exception: not null };

    internal static LazyHelper Create(LazyThreadSafetyMode mode, bool useDefaultConstructor)
        => mode switch {
            LazyThreadSafetyMode.None => !useDefaultConstructor ? NoneViaFactory : NoneViaConstructor,
            LazyThreadSafetyMode.PublicationOnly => !useDefaultConstructor ? PublicationOnlyViaFactory : PublicationOnlyViaConstructor,
            LazyThreadSafetyMode.ExecutionAndPublication => new LazyHelper(useDefaultConstructor ? LazyState.ExecutionAndPublicationViaConstructor : LazyState.ExecutionAndPublicationViaFactory),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), "The mode argument specifies an invalid value.")
        };

    internal static object CreateViaDefaultConstructor(Type type) {
        object obj;
        try {
            obj = Activator.CreateInstance(type);
        }
        catch (MissingMethodException) {
            throw new MissingMemberException("The lazily-initialized type does not have a public, parameterless constructor.");
        }

        return obj;
    }

    internal static LazyThreadSafetyMode GetModeFromIsThreadSafe(bool isThreadSafe) => !isThreadSafe ? LazyThreadSafetyMode.None : LazyThreadSafetyMode.ExecutionAndPublication;

    internal void ThrowException() => throw _exception;

    private LazyThreadSafetyMode GetMode() =>
        State switch {
            LazyState.NoneViaConstructor or LazyState.NoneViaFactory or LazyState.NoneException => LazyThreadSafetyMode.None,
            LazyState.PublicationOnlyViaConstructor or LazyState.PublicationOnlyViaFactory or LazyState.PublicationOnlyWait or LazyState.PublicationOnlyException => LazyThreadSafetyMode.PublicationOnly,
            LazyState.ExecutionAndPublicationViaConstructor or LazyState.ExecutionAndPublicationViaFactory or LazyState.ExecutionAndPublicationException => LazyThreadSafetyMode.ExecutionAndPublication,
            _ => LazyThreadSafetyMode.None
        };
}