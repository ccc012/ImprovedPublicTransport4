using System.Collections.Generic;

namespace System;

public struct ValueTuple<T1, T2> : IEquatable<ValueTuple<T1, T2>>, IComparable, IComparable<ValueTuple<T1, T2>> {
    public T1 Item1;
    public T2 Item2;

    public ValueTuple(T1 item1, T2 item2) {
        Item1 = item1;
        Item2 = item2;
    }

    public override bool Equals(object obj) => obj is ValueTuple<T1, T2> other && Equals(other);

    public override int GetHashCode() {
        var h1 = Item1 == null ? 0 : Item1.GetHashCode();
        var h2 = Item2 == null ? 0 : Item2.GetHashCode();
        return CombineHashCodes(h1, h2);
    }

    public override string ToString() => "(" + (Item1 == null ? "null" : Item1.ToString()) + ", " + (Item2 == null ? "null" : Item2.ToString()) + ")";

    private static int CombineHashCodes(int h1, int h2) {
        unchecked {
            return ((h1 << 5) + h1) ^ h2;
        }
    }

    public bool Equals(ValueTuple<T1, T2> other) => EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);

    public int CompareTo(ValueTuple<T1, T2> other) {
        var c = Comparer<T1>.Default.Compare(Item1, other.Item1);
        if (c != 0) return c;
        return Comparer<T2>.Default.Compare(Item2, other.Item2);
    }

    public void Deconstruct(out T1 item1, out T2 item2) {
        item1 = Item1;
        item2 = Item2;
    }

    int IComparable.CompareTo(object obj) {
        if (obj == null) return 1;
        return obj is not ValueTuple<T1, T2> ? throw new ArgumentException("Object must be ValueTuple<T1,T2>.") : CompareTo((ValueTuple<T1, T2>)obj);
    }
}