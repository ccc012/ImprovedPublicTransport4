// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.SerializableDataExtension
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using System;
using System.IO;
using System.Text;
using ICities;

namespace ImprovedPublicTransport.Data
{
  public class SerializableDataExtension : ISerializableDataExtension
  {
    public static SerializableDataExtension instance;
    private ISerializableData _serializableData;
    private bool _loaded;

    public ISerializableData SerializableData
    {
      get
      {
        return this._serializableData;
      }
    }

    public bool Loaded
    {
      get
      {
        return this._loaded;
      }
      set
      {
        this._loaded = value;
      }
    }

    public event SerializableDataExtension.SaveDataEventHandler EventSaveData;

    public void OnCreated(ISerializableData serializedData)
    {
      SerializableDataExtension.instance = this;
      this._serializableData = serializedData;
    }

    public void OnLoadData()
    {
    }

    public void OnSaveData()
    {
      // ISSUE: reference to a compiler-generated field
      if (!this._loaded || this.EventSaveData == null)
        return;
      // ISSUE: reference to a compiler-generated field
      this.EventSaveData();
    }

    public void OnReleased()
    {
      SerializableDataExtension.instance = (SerializableDataExtension) null;
    }

    public static void WriteByte(byte value, FastList<byte> data)
    {
      data.Add(value);
    }

    public static byte ReadByte(byte[] data, ref int index)
    {
      EnsureAvailable(data, index, 1);
      int num = (int) data[index];
      index = index + 1;
      return (byte) num;
    }

    public static void WriteBool(bool value, FastList<byte> data)
    {
      SerializableDataExtension.AddToData(BitConverter.GetBytes(value), data);
    }

    public static bool ReadBool(byte[] data, ref int index)
    {
      EnsureAvailable(data, index, 1);
      int num = BitConverter.ToBoolean(data, index) ? 1 : 0;
      index = index + 1;
      return num != 0;
    }

    public static void WriteUInt16(ushort value, FastList<byte> data)
    {
      SerializableDataExtension.AddToData(BitConverter.GetBytes(value), data);
    }

    public static ushort ReadUInt16(byte[] data, ref int index)
    {
      EnsureAvailable(data, index, 2);
      int uint16 = (int) BitConverter.ToUInt16(data, index);
      index = index + 2;
      return (ushort) uint16;
    }

    public static void WriteInt32(int value, FastList<byte> data)
    {
      SerializableDataExtension.AddToData(BitConverter.GetBytes(value), data);
    }

    public static int ReadInt32(byte[] data, ref int index)
    {
      EnsureAvailable(data, index, 4);
      int int32 = BitConverter.ToInt32(data, index);
      index = index + 4;
      return int32;
    }

    public static void WriteFloat(float value, FastList<byte> data)
    {
      SerializableDataExtension.AddToData(BitConverter.GetBytes(value), data);
    }

    public static float ReadFloat(byte[] data, ref int index)
    {
      EnsureAvailable(data, index, 4);
      double single = (double) BitConverter.ToSingle(data, index);
      index = index + 4;
      return (float) single;
    }

    public static void WriteString(string s, FastList<byte> data)
    {
      if (s == null)
        throw new ArgumentNullException(nameof(s));
      char[] charArray = s.ToCharArray();
      SerializableDataExtension.WriteInt32(charArray.Length, data);
      for (int index = 0; index < charArray.Length; ++index)
        SerializableDataExtension.AddToData(BitConverter.GetBytes(charArray[index]), data);
    }

    public static string ReadString(byte[] data, ref int index)
    {
      int length = SerializableDataExtension.ReadInt32(data, ref index);
      EnsureCollectionLength(data, index, length, 2);
      var builder = new StringBuilder(length);
      for (int index1 = 0; index1 < length; ++index1)
      {
        builder.Append(BitConverter.ToChar(data, index));
        index = index + 2;
      }
      return builder.ToString();
    }

    public static void WriteFloatArray(float[] array, FastList<byte> data)
    {
      if (array == null)
        throw new ArgumentNullException(nameof(array));
      SerializableDataExtension.WriteInt32(array.Length, data);
      for (int index = 0; index < array.Length; ++index)
        SerializableDataExtension.WriteFloat(array[index], data);
    }

    public static float[] ReadFloatArray(byte[] data, ref int index)
    {
      int length = SerializableDataExtension.ReadInt32(data, ref index);
      EnsureCollectionLength(data, index, length, 4);
      float[] numArray = new float[length];
      for (int index1 = 0; index1 < length; ++index1)
        numArray[index1] = SerializableDataExtension.ReadFloat(data, ref index);
      return numArray;
    }

    public static void AddToData(byte[] bytes, FastList<byte> data)
    {
      foreach (byte num in bytes)
        data.Add(num);
    }

    private static void EnsureAvailable(byte[] data, int index, int byteCount)
    {
      if (data == null)
        throw new ArgumentNullException(nameof(data));
      if (index < 0 || byteCount < 0 || index > data.Length - byteCount)
        throw new EndOfStreamException("Serialized IPT data is truncated or corrupt.");
    }

    private static void EnsureCollectionLength(byte[] data, int index, int length, int bytesPerItem)
    {
      if (length < 0 || length > (data.Length - index) / bytesPerItem)
        throw new InvalidDataException("Serialized IPT collection length is invalid.");
    }

    public delegate void SaveDataEventHandler();
  }
}
