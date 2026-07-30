using System;
using System.Collections.Generic;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl
{
    /// <summary>
    /// Per-building "accept intercity buses" choice, persisted per save. Default is true (accept) -
    /// matching the mod's existing behaviour of patching every eligible station automatically -
    /// so a save made before this setting existed keeps working exactly as before.
    /// </summary>
    /// <remarks>
    /// Only buildings the player has explicitly turned OFF are stored - most cities will have few
    /// or none, so a sparse set is far cheaper than a per-building array.
    /// </remarks>
    internal static class IntercityAcceptanceState
    {
        private const string DataID = "IPT4_IntercityBusAcceptance";
        private static readonly HashSet<ushort> DisabledBuildings = new HashSet<ushort>();
        private static bool _deployed;

        public static bool Accepts(ushort buildingId) => !DisabledBuildings.Contains(buildingId);

        public static void SetAccepts(ushort buildingId, bool accepts)
        {
            if (accepts)
            {
                DisabledBuildings.Remove(buildingId);
            }
            else
            {
                DisabledBuildings.Add(buildingId);
            }
        }

        public static void Init()
        {
            if (_deployed)
            {
                return;
            }

            Load();
            SerializableDataExtension.instance.EventSaveData += OnSaveData;
            _deployed = true;
        }

        public static void Deinit()
        {
            if (!_deployed)
            {
                return;
            }

            DisabledBuildings.Clear();
            SerializableDataExtension.instance.EventSaveData -= OnSaveData;
            _deployed = false;
        }

        private static void Load()
        {
            DisabledBuildings.Clear();
            try
            {
                var raw = SerializableDataExtension.instance.SerializableData.LoadData(DataID);
                if (raw == null || raw.Length < 4)
                {
                    return;
                }

                var index = 0;
                var count = SerializableDataExtension.ReadInt32(raw, ref index);
                for (var i = 0; i < count; i++)
                {
                    DisabledBuildings.Add(SerializableDataExtension.ReadUInt16(raw, ref index));
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"IntercityBusControl: failed to load per-building acceptance data: {ex.Message}");
                DisabledBuildings.Clear();
            }
        }

        private static void OnSaveData()
        {
            try
            {
                var data = new FastList<byte>();
                SerializableDataExtension.WriteInt32(DisabledBuildings.Count, data);
                foreach (var buildingId in DisabledBuildings)
                {
                    SerializableDataExtension.WriteUInt16(buildingId, data);
                }

                SerializableDataExtension.instance.SerializableData.SaveData(DataID, data.ToArray());
            }
            catch (Exception ex)
            {
                Utils.LogError($"IntercityBusControl: failed to save per-building acceptance data: {ex.Message}");
            }
        }
    }
}
