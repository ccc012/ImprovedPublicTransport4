using System.Collections.Generic;

namespace System;

public struct ValueTuple<T1> : IEquatable<ValueTuple<T1>>, IComparable, IComparable<ValueTuple<T1>> {
    public T1 Item1;

    public ValueTuple(T1 item1) => Item1 = item1;

    public override bool Equals(object obj) => obj is ValueTuple<T1> other && Equals(other);
    public override int GetHashCode() => Item1 == null ? 0 : Item1.GetHashCode();

    public override string ToString() => "(" + (Item1 == null ? "null" : Item1.ToString()) + ")";
    public bool Equals(ValueTuple<T1> other) => EqualityComparer<T1>.Default.Equals(Item1, other.Item1);

    public int CompareTo(ValueTuple<T1> other) => Comparer<T1>.Default.Compare(Item1, other.Item1);

    public void Deconstruct(out T1 item1) => item1 = Item1;

    int IComparable.CompareTo(object obj) {
        if (obj == null) return 1;
        return obj is not ValueTuple<T1> ? throw new ArgumentException("Object must be ValueTuple<T1>.") : CompareTo((ValueTuple<T1>)obj);
    }
}