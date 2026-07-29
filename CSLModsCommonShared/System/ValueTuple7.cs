using System.Collections.Generic;

namespace System;

public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7> :
    IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>,
    IComparable,
    IComparable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> {
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;

    public ValueTuple(
        T1 item1, T2 item2, T3 item3,
        T4 item4, T5 item5, T6 item6, T7 item7) {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
    }

    public override bool Equals(object obj) => obj is ValueTuple<T1, T2, T3, T4, T5, T6, T7> other && Equals(other);

    public override int GetHashCode() {
        var h1 = Item1 == null ? 0 : Item1.GetHashCode();
        var h2 = Item2 == null ? 0 : Item2.GetHashCode();
        var h3 = Item3 == null ? 0 : Item3.GetHashCode();
        var h4 = Item4 == null ? 0 : Item4.GetHashCode();
        var h5 = Item5 == null ? 0 : Item5.GetHashCode();
        var h6 = Item6 == null ? 0 : Item6.GetHashCode();
        var h7 = Item7 == null ? 0 : Item7.GetHashCode();

        var h12 = CombineHashCodes(h1, h2);
        var h34 = CombineHashCodes(h3, h4);
        var h56 = CombineHashCodes(h5, h6);

        return CombineHashCodes(CombineHashCodes(h12, h34), CombineHashCodes(h56, h7));
    }

    public override string ToString() =>
        "(" +
        (Item1 == null ? "null" : Item1.ToString()) + ", " +
        (Item2 == null ? "null" : Item2.ToString()) + ", " +
        (Item3 == null ? "null" : Item3.ToString()) + ", " +
        (Item4 == null ? "null" : Item4.ToString()) + ", " +
        (Item5 == null ? "null" : Item5.ToString()) + ", " +
        (Item6 == null ? "null" : Item6.ToString()) + ", " +
        (Item7 == null ? "null" : Item7.ToString()) +
        ")";

    private static int CombineHashCodes(int h1, int h2) {
        unchecked {
            return ((h1 << 5) + h1) ^ h2;
        }
    }

    public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7> other) =>
        EqualityComparer<T1>.Default.Equals(Item1, other.Item1) &&
        EqualityComparer<T2>.Default.Equals(Item2, other.Item2) &&
        EqualityComparer<T3>.Default.Equals(Item3, other.Item3) &&
        EqualityComparer<T4>.Default.Equals(Item4, other.Item4) &&
        EqualityComparer<T5>.Default.Equals(Item5, other.Item5) &&
        EqualityComparer<T6>.Default.Equals(Item6, other.Item6) &&
        EqualityComparer<T7>.Default.Equals(Item7, other.Item7);

    public int CompareTo(ValueTuple<T1, T2, T3, T4, T5, T6, T7> other) {
        var c = Comparer<T1>.Default.Compare(Item1, other.Item1);
        if (c != 0) return c;

        c = Comparer<T2>.Default.Compare(Item2, other.Item2);
        if (c != 0) return c;

        c = Comparer<T3>.Default.Compare(Item3, other.Item3);
        if (c != 0) return c;

        c = Comparer<T4>.Default.Compare(Item4, other.Item4);
        if (c != 0) return c;

        c = Comparer<T5>.Default.Compare(Item5, other.Item5);
        if (c != 0) return c;

        c = Comparer<T6>.Default.Compare(Item6, other.Item6);
        if (c != 0) return c;

        return Comparer<T7>.Default.Compare(Item7, other.Item7);
    }

    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7) {
        item1 = Item1;
        item2 = Item2;
        item3 = Item3;
        item4 = Item4;
        item5 = Item5;
        item6 = Item6;
        item7 = Item7;
    }

    int IComparable.CompareTo(object obj) {
        if (obj == null) return 1;
        return obj is not ValueTuple<T1, T2, T3, T4, T5, T6, T7> ? throw new ArgumentException("Object must be ValueTuple<T1,T2,T3,T4,T5,T6,T7>.") : CompareTo((ValueTuple<T1, T2, T3, T4, T5, T6, T7>)obj);
    }
}