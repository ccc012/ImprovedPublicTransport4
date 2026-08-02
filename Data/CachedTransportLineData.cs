using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Data
{
    public static class CachedTransportLineData
    {
        private static readonly string _dataID = "ImprovedPublicTransport";
        private static readonly string _dataVersion = "v004";

        public static bool _init;
        public static LineData[] _lineData;
        
        public static void Init()
        {
            if (!TryLoadData(out _lineData))
            {
                Utils.Log("Loading default transport line data.");
                NetManager instance1 = Singleton<NetManager>.instance;
                TransportManager instance2 = Singleton<TransportManager>.instance;
                // _lineData is fixed at 256 slots everywhere else (TryLoadData/OnSaveData both
                // hardcode it) - vanilla TransportManager.m_lines is also 256, but bounding this
                // loop by the array's own length instead of the live buffer's is what actually
                // guarantees no IndexOutOfRangeException here if that assumption ever breaks (a
                // mod expanding the line cap, a future game update, etc.).
                int length = Mathf.Min(instance2.m_lines.m_buffer.Length, _lineData.Length);
                for (ushort index = 0; index < length; ++index)
                {
                    if (instance2.m_lines.m_buffer[index].Complete)
                    {
                        _lineData[index].TargetVehicleCount = TransportLineUtil.CountLineActiveVehicles(index, out int _);
                    }
                    else
                        _lineData[index].TargetVehicleCount =
                            ModSetting.Instance.DefaultVehicleCount;
                    _lineData[index].BudgetControl = ModSetting.Instance.BudgetControl == ModSetting.BudgetControlModes.Enabled;
                    _lineData[index].Depot = DepotUtil.GetClosestDepot(index,
                        instance1.m_nodes.m_buffer[instance2.m_lines.m_buffer[index].GetStop(0)].m_position);
                    _lineData[index].Unbunching = ModSetting.Instance.Unbunching;
                }
            }
            SerializableDataExtension.instance.EventSaveData += OnSaveData;

            _init = true;
        }

        public static void Deinit()
        {
            _lineData = null;
            SerializableDataExtension.instance.EventSaveData -= OnSaveData;
            _init = false;
        }

        public static bool TryLoadData(out LineData[] data)
        {
            data = new LineData[256];
            var data1 = SerializableDataExtension.instance.SerializableData.LoadData(_dataID);
            if (data1 == null)
                return false;
            var index1 = 0;
            ushort lineID = 0;
            try
            {
                Utils.Log("Try to load transport line data.");
                var str = SerializableDataExtension.ReadString(data1, ref index1);
                if (string.IsNullOrEmpty(str) || str.Length != 4)
                {
                    Utils.LogWarning("Unknown data found.");
                    return false;
                }
                Utils.Log("Found transport line data version: " + str);
                var instance1 = Singleton<NetManager>.instance;
                var instance2 = Singleton<TransportManager>.instance;
                while (index1 < data1.Length)
                {
                    if (instance2.m_lines.m_buffer[lineID].Complete)
                    {
                        var int32 = BitConverter.ToInt32(data1, index1);
                        data[lineID].TargetVehicleCount = int32;
                    }
                    index1 += 4;
                    var num = Mathf.Min(BitConverter.ToSingle(data1, index1),
                        ModSetting.Instance.SpawnTimeInterval);
                    if (num > 0.0)
                        data[lineID].NextSpawnTime = SimHelper.SimulationTime + num;
                    index1 += 4;
                    var boolean = BitConverter.ToBoolean(data1, index1);
                    data[lineID].BudgetControl = boolean;
                    ++index1;
                    var uint16 = BitConverter.ToUInt16(data1, index1);
                    data[lineID].Depot = uint16 != 0
                        ? uint16
                        : DepotUtil.GetClosestDepot(lineID,
                            instance1.m_nodes.m_buffer[instance2.m_lines.m_buffer[lineID].GetStop(0)]
                                .m_position);
                    index1 += 2;
                    if (str == "v001")
                    {
                        var name = SerializableDataExtension.ReadString(data1, ref index1);
                        if (name != "Random")
                        {
                            data[lineID].Prefabs ??= new HashSet<string>();
                            if (PrefabCollection<VehicleInfo>.FindLoaded(name) !=
                                null)
                                data[lineID].Prefabs.Add(name);
                        }
                    }
                    else
                    {
                        var int32 = BitConverter.ToInt32(data1, index1);
                        index1 += 4;
                        for (var index2 = 0; index2 < int32; ++index2)
                        {
                            var name = SerializableDataExtension.ReadString(data1, ref index1);
                            data[lineID].Prefabs ??= new HashSet<string>();
                            if (PrefabCollection<VehicleInfo>.FindLoaded(name) !=
                                null)
                                data[lineID].Prefabs.Add(name);
                        }
                    }
                    if (str != "v001")
                    {
                        var int32 = BitConverter.ToInt32(data1, index1);
                        index1 += 4;
                        for (var index2 = 0; index2 < int32; ++index2)
                        {
                            var name = SerializableDataExtension.ReadString(data1, ref index1);
                            if (boolean)
                            {
                                continue;
                            }
                            data[lineID].QueuedVehicles ??= new Queue<string>();
                            if (PrefabCollection<VehicleInfo>.FindLoaded(name) == null)
                            {
                                continue;
                            }
                            lock (data[lineID].QueuedVehicles) 
                                data[lineID].QueuedVehicles.Enqueue(name);
                        }
                    }
                    if (str == "v003")
                        ++index1;
                    data[lineID].Unbunching = str != "v004"
                        ? ModSetting.Instance.Unbunching
                        : SerializableDataExtension.ReadBool(data1, ref index1);
                    ++lineID;
                }
                return true;
            }
            catch (Exception ex)
            {
                Utils.LogWarning("Could not load transport line data. " + ex.Message);
                data = new LineData[256];
                return false;
            }
        }

        private static void OnSaveData()
        {
            var data = new FastList<byte>();
            try
            {
                SerializableDataExtension.WriteString(_dataVersion, data);
                for (ushort lineID = 0; lineID < 256; ++lineID)
                {
                    SerializableDataExtension.AddToData(
                        BitConverter.GetBytes(GetTargetVehicleCount(lineID)), data);
                    SerializableDataExtension.AddToData(
                        BitConverter.GetBytes(Mathf.Max(
                            GetNextSpawnTime(lineID) - SimHelper.SimulationTime, 0.0f)),
                        data);
                    SerializableDataExtension.AddToData(
                        BitConverter.GetBytes(GetBudgetControlState(lineID)), data);
                    SerializableDataExtension.AddToData(BitConverter.GetBytes(GetDepot(lineID)), data);
                    var num = 0;
                    var prefabs = GetPrefabs(lineID);
                    if (prefabs != null)
                        num = prefabs.Count;
                    SerializableDataExtension.AddToData(BitConverter.GetBytes(num), data);
                    if (num > 0)
                    {
                        foreach (var s in prefabs)
                            SerializableDataExtension.WriteString(s, data);
                    }
                    var enqueuedVehicles = GetEnqueuedVehicles(lineID);
                    SerializableDataExtension.AddToData(BitConverter.GetBytes(enqueuedVehicles.Length), data);
                    if (enqueuedVehicles.Length != 0)
                    {
                        foreach (var s in enqueuedVehicles)
                            SerializableDataExtension.WriteString(s, data);
                    }
                    SerializableDataExtension.WriteBool(GetUnbunchingState(lineID), data);
                }
                SerializableDataExtension.instance.SerializableData.SaveData(_dataID, data.ToArray());
            }
            catch (Exception ex)
            {
                var msg = "Error while saving transport line data! " + ex.Message + " " + ex.InnerException;
                Utils.LogError(msg);
                CODebugBase<LogChannel>.Log(LogChannel.Modding, msg, ErrorLevel.Error);
            }
        }
        
        private static bool IsValidLineId(ushort lineID) =>
            _init && _lineData != null && lineID < _lineData.Length;

        public static int GetTargetVehicleCount(ushort lineID)
        {
            return IsValidLineId(lineID) ? _lineData[lineID].TargetVehicleCount : 0;
        }
                
        public static void SetLineDefaults(ushort lineID)
        {
            if (!IsValidLineId(lineID))
                return;
            _lineData[lineID] = new LineData
            {
                TargetVehicleCount = ModSetting.Instance.DefaultVehicleCount,
                BudgetControl = ModSetting.Instance.BudgetControl == ModSetting.BudgetControlModes.Enabled,
                Unbunching = ModSetting.Instance.Unbunching
            };
        }

        public static void SetTargetVehicleCount(ushort lineID, int count)
        {
            if (!IsValidLineId(lineID))
                return;
            _lineData[lineID].TargetVehicleCount = count;
        }

        public static void IncreaseTargetVehicleCount(ushort lineID)
        {
            if (!IsValidLineId(lineID))
                return;
            ++_lineData[lineID].TargetVehicleCount;
        }

        public static void DecreaseTargetVehicleCount(ushort lineID)
        {
            if (!IsValidLineId(lineID) || _lineData[lineID].TargetVehicleCount == 0)
                return;
            --_lineData[lineID].TargetVehicleCount;
        }

        public static float GetNextSpawnTime(ushort lineID)
        {
            return IsValidLineId(lineID) ? _lineData[lineID].NextSpawnTime : 0f;
        }

        public static void SetNextSpawnTime(ushort lineID, float time)
        {
            if (!IsValidLineId(lineID))
                return;
            _lineData[lineID].NextSpawnTime = time;
        }

        public static bool GetBudgetControlState(ushort lineID)
        {
            // Default true (vanilla budget) when cache is not ready — safer than IndexOutOfRange/NRE.
            return !IsValidLineId(lineID) || _lineData[lineID].BudgetControl;
        }

        public static void SetBudgetControlState(ushort lineID, bool state)
        {
            if (!IsValidLineId(lineID))
                return;
            _lineData[lineID].BudgetControl = state;
        }

        public static bool GetUnbunchingState(ushort lineID)
        {
            return IsValidLineId(lineID) && _lineData[lineID].Unbunching;
        }

        public static void SetUnbunchingState(ushort lineID, bool state)
        {
            if (!IsValidLineId(lineID))
                return;
            _lineData[lineID].Unbunching = state;
        }

        public static ushort GetDepot(ushort lineID)
        {
            return IsValidLineId(lineID) ? _lineData[lineID].Depot : (ushort)0;
        }

        public static void SetDepot(ushort lineID, ushort depotID)
        {
            if (!IsValidLineId(lineID))
                return;
            _lineData[lineID].Depot = depotID;
        }

        public static HashSet<string> GetPrefabs(ushort lineID)
        {
            return IsValidLineId(lineID) ? _lineData[lineID].Prefabs : null;
        }

        public static void SetPrefabs(ushort lineID, HashSet<string> prefabs)
        {
            if (!IsValidLineId(lineID))
                return;
            _lineData[lineID].Prefabs = prefabs;
        }

        public static string GetRandomPrefab(ushort lineID)
        {
            if (!IsValidLineId(lineID))
                return null;

            if (_lineData[lineID].Prefabs != null)
            {
                var linePrefabs = _lineData[lineID].Prefabs;
                int count = linePrefabs.Count;
                if (count != 0)
                {
                    var index = (int)Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)count);
                    int i = 0;
                    foreach (var name in linePrefabs)
                    {
                        if (i++ == index) return name;
                    }
                }
            }
            var info = Singleton<TransportManager>.instance.m_lines.m_buffer[lineID].Info;
            if (info?.m_class == null || VehiclePrefabs.instance == null)
                return null;
            var itemClass = info.m_class;
            var prefabs = VehiclePrefabs.instance.GetPrefabs(itemClass.m_service, itemClass.m_subService, itemClass.m_level);
            if (prefabs == null || prefabs.Length == 0)
                return null;
            var index1 = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint) prefabs.Length);
            return prefabs[index1].Name;
        }

        public static void EnqueueVehicle(ushort lineID, string prefabName)
        {
            if (!IsValidLineId(lineID) || string.IsNullOrEmpty(prefabName))
                return;
            _lineData[lineID].QueuedVehicles ??= new Queue<string>();
            lock (_lineData[lineID].QueuedVehicles)
                _lineData[lineID].QueuedVehicles.Enqueue(prefabName);
        }

        public static string Dequeue(ushort lineID)
        {
            if (!IsValidLineId(lineID) || _lineData[lineID].QueuedVehicles is not { Count: not 0 })
            {
                return null;
            }
            lock (_lineData[lineID].QueuedVehicles)
                return _lineData[lineID].QueuedVehicles.Dequeue();
        }

        public static void DequeueVehicle(ushort lineID)
        {
            if (!IsValidLineId(lineID) || _lineData[lineID].QueuedVehicles is not { Count: not 0 })
            {
                return;
            }

            DecreaseTargetVehicleCount(lineID);
            Dequeue(lineID);
        }

        public static void DequeueVehicles(ushort lineID, int[] indexes, bool decreaseVehicleCount = true)
        {
            if (!IsValidLineId(lineID) || _lineData[lineID].QueuedVehicles is not { Count: not 0 } || indexes == null || indexes.Length == 0)
            {
                return;
            }
            lock (_lineData[lineID].QueuedVehicles)
            {
                var stringList = new List<string>(_lineData[lineID].QueuedVehicles);
                var validIndexes = new List<int>(indexes.Length);
                foreach (int selectedIndex in indexes)
                {
                    if (selectedIndex >= 0 && selectedIndex < stringList.Count && !validIndexes.Contains(selectedIndex))
                    {
                        validIndexes.Add(selectedIndex);
                    }
                }

                if (validIndexes.Count == 0)
                {
                    return;
                }

                validIndexes.Sort();
                for (int index = validIndexes.Count - 1; index >= 0; --index)
                {
                    stringList.RemoveAt(validIndexes[index]);
                    if (decreaseVehicleCount)
                    {
                        DecreaseTargetVehicleCount(lineID);
                    }
                }

                _lineData[lineID].QueuedVehicles = new Queue<string>(stringList);
            }
        }

        public static string[] GetEnqueuedVehicles(ushort lineID)
        {
            if (!IsValidLineId(lineID) || _lineData[lineID].QueuedVehicles is not { Count: not 0 })
                return new string[0];
            lock (_lineData[lineID].QueuedVehicles)
                return _lineData[lineID].QueuedVehicles.ToArray();
        }

        public static int EnqueuedVehiclesCount(ushort lineID)
        {
            if (!IsValidLineId(lineID) || _lineData[lineID].QueuedVehicles == null)
                return 0;
            return _lineData[lineID].QueuedVehicles.Count;
        }

        public static void ClearEnqueuedVehicles(ushort lineID)
        {
            if (!IsValidLineId(lineID) || _lineData[lineID].QueuedVehicles is not { Count: > 0 })
                return;
            lock (_lineData[lineID].QueuedVehicles)
                _lineData[lineID].QueuedVehicles.Clear();
        }

    }
}

