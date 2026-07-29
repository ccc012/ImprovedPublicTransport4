using System.Collections.Generic;

namespace System;

public struct ValueTuple<T1, T2, T3, T4> : IEquatable<ValueTuple<T1, T2, T3, T4>>, IComparable, IComparable<ValueTuple<T1, T2, T3, T4>> {
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;

    public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4) {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
    }

    public override bool Equals(object obj) => obj is ValueTuple<T1, T2, T3, T4> other && Equals(other);

    public override int GetHashCode() {
        var h1 = Item1 == null ? 0 : Item1.GetHashCode();
        var h2 = Item2 == null ? 0 : Item2.GetHashCode();
        var h3 = Item3 == null ? 0 : Item3.GetHashCode();
        var h4 = Item4 == null ? 0 : Item4.GetHashCode();
        return CombineHashCodes(CombineHashCodes(h1, h2), CombineHashCodes(h3, h4));
    }

    public override string ToString() => "(" + (Item1 == null ? "null" : Item1.ToString()) + ", " + (Item2 == null ? "null" : Item2.ToString()) + ", " + (Item3 == null ? "null" : Item3.ToString()) + ", " + (Item4 == null ? "null" : Item4.ToString()) + ")";

    private static int CombineHashCodes(int h1, int h2) {
        unchecked {
            return ((h1 << 5) + h1) ^ h2;
        }
    }

    public bool Equals(ValueTuple<T1, T2, T3, T4> other) => EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2) && EqualityComparer<T3>.Default.Equals(Item3, other.Item3) && EqualityComparer<T4>.Default.Equals(Item4, other.Item4);

    public int CompareTo(ValueTuple<T1, T2, T3, T4> other) {
        var c = Comparer<T1>.Default.Compare(Item1, other.Item1);
        if (c != 0) return c;
        c = Comparer<T2>.Default.Compare(Item2, other.Item2);
        if (c != 0) return c;
        c = Comparer<T3>.Default.Compare(Item3, other.Item3);
        if (c != 0) return c;
        return Comparer<T4>.Default.Compare(Item4, other.Item4);
    }

    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4) {
        item1 = Item1;
        item2 = Item2;
        item3 = Item3;
        item4 = Item4;
    }

    int IComparable.CompareTo(object obj) {
        if (obj == null) return 1;
        return obj is not ValueTuple<T1, T2, T3, T4> ? throw new ArgumentException("Object must be ValueTuple<T1,T2,T3,T4>.") : CompareTo((ValueTuple<T1, T2, T3, T4>)obj);
    }
}