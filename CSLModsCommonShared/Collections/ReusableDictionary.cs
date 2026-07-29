using System;
using System.Collections;
using System.Collections.Generic;

namespace CSLModsCommon.Collections; 
public class ReusableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReusable {
    private static readonly Stack<ReusableDictionary<TKey, TValue>> _pool = new();
    private static readonly object _lock = new();
    private Dictionary<TKey, TValue> _dict;

    public static int MaxPoolSize { get; set; } = 100;
    public bool IsDisposed { get; private set; }
    public int RentCount { get; private set; }
    public int Count => _dict.Count;
    public bool IsReadOnly => false;

    public static ReusableDictionary<TKey, TValue> Rent(int capacity = 4) {
        lock (_lock) {
            if (_pool.Count > 0) {
                var instance = _pool.Pop();
                instance.OnRent(capacity);
                return instance;
            }
        }

        var newInstance = new ReusableDictionary<TKey, TValue>(capacity);
        newInstance.OnRent(capacity);
        return newInstance;
    }

    public static void Return(ReusableDictionary<TKey, TValue> instance) {
        lock (_lock) {
            if (_pool.Count >= MaxPoolSize) {
                instance.Destroy();
                return;
            }

            instance.OnReturn();
            _pool.Push(instance);
        }
    }

    private ReusableDictionary(int capacity) {
        _dict = new Dictionary<TKey, TValue>(capacity);
        IsDisposed = false;
    }

    public TValue this[TKey key] {
        get {
            CheckDisposed();
            return _dict[key];
        }
        set {
            CheckDisposed();
            _dict[key] = value;
        }
    }

    public ICollection<TKey> Keys => _dict.Keys;
    public ICollection<TValue> Values => _dict.Values;

    public void Add(TKey key, TValue value) {
        CheckDisposed();
        _dict.Add(key, value);
    }

    public bool ContainsKey(TKey key) {
        CheckDisposed();
        return _dict.ContainsKey(key);
    }

    public bool Remove(TKey key) {
        CheckDisposed();
        return _dict.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value) {
        CheckDisposed();
        return _dict.TryGetValue(key, out value);
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        CheckDisposed();
        return _dict.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(_dict[item.Key], item.Value);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        CheckDisposed();
        ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        CheckDisposed();
        if (Contains(item)) return _dict.Remove(item.Key);

        return false;
    }

    public void Clear() {
        CheckDisposed();
        _dict.Clear();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        CheckDisposed();
        return _dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Return() {
        if (IsDisposed) return;
        Return(this);
    }

    public void Dispose() => Return();

    public void Destroy() {
        if (!IsDisposed) {
            Clear();
            IsDisposed = true;
        }

        _dict = null!;
    }

    public void OnRent() => OnRent(4);

    public void OnRent(int capacity) {
        IsDisposed = false;
        RentCount++;
        if (_dict == null || _dict.Count < capacity)
            _dict = new Dictionary<TKey, TValue>(capacity);
        Clear();
    }

    public void OnReturn() {
        Clear();
        IsDisposed = true;
    }

    private void CheckDisposed() {
        if (IsDisposed || _dict == null)
            throw new ObjectDisposedException(nameof(ReusableDictionary<TKey, TValue>));
    }

    public override string ToString() => $"ReusableDictionary<{typeof(TKey).Name},{typeof(TValue).Name}>({Count}) RentCount={RentCount} IsDisposed={IsDisposed}";
}