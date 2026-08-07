// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.MovingAverage
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using System.Collections.Generic;

namespace ImprovedPublicTransport.Data
{
  public class MovingAverage
  {
    private int _sampleLenght;
    private Queue<float> _items;

    public int SampleLenght
    {
      get
      {
        return this._sampleLenght;
      }
      set
      {
        value = System.Math.Max(1, value);
        if (this._sampleLenght == value)
          return;
        this._sampleLenght = value;
      }
    }

    public float Average
    {
      get
      {
        lock (this._items)
        {
          if (this._items.Count == 0)
            return 0.0f;
          // Manual average — LINQ enumerator alloc on every weekly/stats read.
          float sum = 0f;
          int n = 0;
          foreach (var v in this._items)
          {
            sum += v;
            n++;
          }
          return n == 0 ? 0f : sum / n;
        }
      }
    }

    public MovingAverage()
      : this(10)
    {
    }

    public MovingAverage(int sampleLenght)
    {
      this._sampleLenght = System.Math.Max(1, sampleLenght);
      this._items = new Queue<float>(this._sampleLenght);
    }

    public MovingAverage(float[] array, int sampleLenght)
    {
      this._sampleLenght = System.Math.Max(1, sampleLenght);
      this._items = new Queue<float>();
      if (array == null)
        return;
      int start = System.Math.Max(0, array.Length - this._sampleLenght);
      for (int i = start; i < array.Length; i++)
        this._items.Enqueue(array[i]);
    }

    public void Clear()
    {
      lock (this._items)
        this._items.Clear();
    }

    public void Push(float value)
    {
      lock (this._items)
      {
        while (this._items.Count >= this._sampleLenght)
          this._items.Dequeue();
        this._items.Enqueue(value);
      }
    }

    public float[] ToArray()
    {
      lock (this._items)
        return this._items.ToArray();
    }
  }
}
