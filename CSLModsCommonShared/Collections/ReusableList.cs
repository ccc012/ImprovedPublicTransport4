using System;
using System.Collections;
using System.Collections.Generic;

namespace CSLModsCommon.Collections;

public class ReusableList<T> : IList<T>, IReusable, IReadOnlyList<T> {
    public static int MaxPoolSize { get; set; } = 100;
    private static readonly T[] EmptyArray = new T[0];
    private static readonly Stack<ReusableList<T>> Pool = new();
    private static readonly object Lock = new();

    private T[] _items;

    public bool IsReadOnly => false;
    public bool IsDisposed { get; private set; }
    public int Count { get; private set; }
    public int RentCount { get; private set; }

    public static ReusableList<T> Rent(int capacity = 4) {
        lock (Lock) {
            if (Pool.Count > 0) {
                var instance = Pool.Pop();
                instance.OnRent(capacity);
                return instance;
            }
        }

        var newInstance = new ReusableList<T>(capacity);
        newInstance.OnRent(capacity);
        return newInstance;
    }

    public static void Return(ReusableList<T> instance) {
        lock (Lock) {
            if (Pool.Count >= MaxPoolSize) {
                instance.Destroy();
                return;
            }

            instance.OnReturn();
            Pool.Push(instance);
        }
    }

    private ReusableList(int capacity) {
        _items = new T[capacity];
        Count = 0;
        IsDisposed = false;
    }

    public T this[int index] {
        get {
            CheckDisposed();
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException();
            return _items[index];
        }
        set {
            CheckDisposed();
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException();
            _items[index] = value;
        }
    }

    public void Add(T item) {
        CheckDisposed();
        EnsureCapacity(Count + 1);
        _items[Count++] = item;
    }

    public bool Contains(T item) {
        CheckDisposed();
        return IndexOf(item) >= 0;
    }

    public int IndexOf(T item) {
        CheckDisposed();
        for (var i = 0; i < Count; i++)
            if (Equals(_items[i], item))
                return i;

        return -1;
    }

    public void Insert(int index, T item) {
        CheckDisposed();
        if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        EnsureCapacity(Count + 1);
        if (index < Count)
            Array.Copy(_items, index, _items, index + 1, Count - index);
        _items[index] = item;
        Count++;
    }

    public bool Remove(T item) {
        CheckDisposed();
        for (var i = 0; i < Count; i++) {
            if (!Equals(_items[i], item)) continue;
            var moveCount = Count - i - 1;
            if (moveCount > 0)
                Array.Copy(_items, i + 1, _items, i, moveCount);
            _items[--Count] = default!;
            return true;
        }

        return false;
    }

    public void RemoveAt(int index) {
        CheckDisposed();
        if ((uint)index >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        var moveCount = Count - index - 1;
        if (moveCount > 0)
            Array.Copy(_items, index + 1, _items, index, moveCount);
        _items[--Count] = default!;
    }

    public void CopyTo(T[] array, int arrayIndex) {
        CheckDisposed();
        Array.Copy(_items, 0, array, arrayIndex, Count);
    }

    public IEnumerator<T> GetEnumerator() {
        CheckDisposed();
        for (var i = 0; i < Count; i++)
            yield return _items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Clear() {
        CheckDisposed();
        Array.Clear(_items, 0, Count);
        Count = 0;
    }

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

        _items = EmptyArray;
    }

    public void OnRent() => OnRent(4);

    public void OnRent(int capacity) {
        IsDisposed = false;
        RentCount++;
        if (_items.Length < capacity)
            _items = new T[capacity];
        Clear();
    }

    public void OnReturn() {
        Clear();
        IsDisposed = true;
    }

    private void EnsureCapacity(int min) {
        if (_items.Length >= min) return;
        var newCapacity = Math.Max(min, _items.Length * 2);
        var newArr = new T[newCapacity];
        Array.Copy(_items, 0, newArr, 0, Count);
        _items = newArr;
    }

    private void CheckDisposed() {
        if (IsDisposed || _items == null)
            throw new ObjectDisposedException(nameof(ReusableList<T>));
    }

    public override string ToString() => $"ReusableList<{typeof(T).Name}>({Count}/{_items.Length}) RentCount={RentCount} IsDisposed={IsDisposed}";
}