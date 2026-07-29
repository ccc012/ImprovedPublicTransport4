using System.Threading;

namespace System;

public class Lazy<T> {
    private volatile LazyHelper _state;
    private Func<T> _factory;
    private T _value;

    internal T ValueForDebugDisplay => !IsValueCreated ? default : _value;

    internal LazyThreadSafetyMode? Mode => LazyHelper.GetMode(_state);

    internal bool IsValueFaulted => LazyHelper.GetIsValueFaulted(_state);

    public bool IsValueCreated => _state == null;

    public T Value => _state != null ? CreateValue() : _value;


    public Lazy() : this(null, LazyThreadSafetyMode.ExecutionAndPublication, true) { }

    public Lazy(T value) => _value = value;

    public Lazy(Func<T> valueFactory) : this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication, false) { }

    public Lazy(bool isThreadSafe) : this(null, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), true) { }

    public Lazy(LazyThreadSafetyMode mode) : this(null, mode, true) { }

    public Lazy(Func<T> valueFactory, bool isThreadSafe) : this(valueFactory, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), false) { }

    public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode) : this(valueFactory, mode, false) { }

    private Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode, bool useDefaultConstructor) {
        if (valueFactory == null && !useDefaultConstructor) {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        _factory = valueFactory;
        _state = LazyHelper.Create(mode, useDefaultConstructor);
    }

    public override string ToString() {
        if (!IsValueCreated) {
            return "Value is not created.";
        }

        var value = Value;
        return value.ToString();
    }

    private static T CreateViaDefaultConstructor() => (T)LazyHelper.CreateViaDefaultConstructor(typeof(T));

    private void ViaConstructor() {
        _value = CreateViaDefaultConstructor();
        _state = null;
    }

    private void ViaFactory(LazyThreadSafetyMode mode) {
        try {
            var factory = _factory;
            if (factory == null) {
                throw new InvalidOperationException("ValueFactory attempted to access the Value property of this instance.");
            }

            _factory = null;
            _value = factory();
            _state = null;
        }
        catch (Exception ex) {
            _state = new LazyHelper(mode, ex);
            throw;
        }
    }

    private void ExecutionAndPublication(LazyHelper executionAndPublication, bool useDefaultConstructor) {
        lock (executionAndPublication) {
            if (_state != executionAndPublication) return;
            if (useDefaultConstructor) {
                ViaConstructor();
            }
            else {
                ViaFactory(LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }
    }

    private void PublicationOnly(LazyHelper publicationOnly, T possibleValue) {
        if (Interlocked.CompareExchange(ref _state, LazyHelper.PublicationOnlyWaitForOtherThreadToPublish, publicationOnly) != publicationOnly) return;
        _factory = null;
        _value = possibleValue;
        _state = null;
    }

    private void PublicationOnlyViaConstructor(LazyHelper initializer) => PublicationOnly(initializer, CreateViaDefaultConstructor());

    private void PublicationOnlyViaFactory(LazyHelper initializer) {
        var factory = _factory;
        if (factory == null) {
            PublicationOnlyWaitForOtherThreadToPublish();
            return;
        }

        PublicationOnly(initializer, factory());
    }

    private void PublicationOnlyWaitForOtherThreadToPublish() {
        var spinWait = default(SpinWait);
        while (_state != null) {
            spinWait.SpinOnce();
        }
    }

    private T CreateValue() {
        var state = _state;
        if (state == null) return _value;
        switch (state.State) {
            case LazyState.NoneViaConstructor: ViaConstructor(); break;
            case LazyState.NoneViaFactory: ViaFactory(LazyThreadSafetyMode.None); break;
            case LazyState.PublicationOnlyViaConstructor: PublicationOnlyViaConstructor(state); break;
            case LazyState.PublicationOnlyViaFactory: PublicationOnlyViaFactory(state); break;
            case LazyState.PublicationOnlyWait: PublicationOnlyWaitForOtherThreadToPublish(); break;
            case LazyState.ExecutionAndPublicationViaConstructor: ExecutionAndPublication(state, true); break;
            case LazyState.ExecutionAndPublicationViaFactory: ExecutionAndPublication(state, false); break;
            case LazyState.NoneException:
            case LazyState.PublicationOnlyException:
            case LazyState.ExecutionAndPublicationException:
            default: state.ThrowException(); break;
        }

        return _value;
    }
}