using System;
using System.Collections.Generic;
using ColossalFramework;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace IntercityBusControl
{
    /// <summary>
    /// Per-building "accept intercity buses" choice.
    /// Defaults: native Intercity Bus Stations = ON; converted simple stations = OFF;
    /// explicit user clicks override via Enabled/Disabled sets.
    /// </summary>
    internal static class IntercityAcceptanceState
    {
        private const string DataID = "IPT4_IntercityBusAcceptance";
        private const string DataIDEnabled = "IPT4_IntercityBusAcceptanceOn";

        /// <summary>User (or default for converted) rejected intercity buses.</summary>
        private static readonly HashSet<ushort> DisabledBuildings = new HashSet<ushort>();

        /// <summary>User forced accept on a building that would otherwise default OFF.</summary>
        private static readonly HashSet<ushort> EnabledBuildings = new HashSet<ushort>();

        private static bool _deployed;

        public static bool Accepts(ushort buildingId)
        {
            if (DisabledBuildings.Contains(buildingId))
            {
                return false;
            }

            if (EnabledBuildings.Contains(buildingId))
            {
                return true;
            }

            // No explicit override: default by prefab class.
            return DefaultAccepts(buildingId);
        }

        /// <summary>
        /// Native SH intermunicipal stations default ON; converted simple stations default OFF.
        /// </summary>
        public static bool DefaultAccepts(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            var info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            if (info == null)
            {
                return true;
            }

            var ai = info.m_buildingAI as TransportStationAI;
            if (ai != null && StationPatcher.IsNativeIntercityBusStation(info, ai))
            {
                return true;
            }

            // Converted / simple bus stations that we wired for intercity: default OFF.
            if (StationPatcher.PrefabsDefaultReject.Contains(info.name)
                || (StationPatcher.PatchedBuildingNames.Contains(info.name)
                    && (ai == null || !StationPatcher.IsNativeIntercityBusStation(info, ai))))
            {
                return false;
            }

            return true;
        }

        public static void SetAccepts(ushort buildingId, bool accepts)
        {
            if (accepts)
            {
                DisabledBuildings.Remove(buildingId);
                // If default is already ON, no need to store Enabled.
                if (!DefaultAcceptsWithoutOverrides(buildingId))
                {
                    EnabledBuildings.Add(buildingId);
                }
                else
                {
                    EnabledBuildings.Remove(buildingId);
                }
            }
            else
            {
                EnabledBuildings.Remove(buildingId);
                // If default is already OFF, no need to store Disabled.
                if (DefaultAcceptsWithoutOverrides(buildingId))
                {
                    DisabledBuildings.Add(buildingId);
                }
                else
                {
                    DisabledBuildings.Remove(buildingId);
                }
            }
        }

        /// <summary>Default without consulting Enabled/Disabled sparse sets.</summary>
        private static bool DefaultAcceptsWithoutOverrides(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            var info = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            if (info == null)
            {
                return true;
            }

            var ai = info.m_buildingAI as TransportStationAI;
            if (ai != null && StationPatcher.IsNativeIntercityBusStation(info, ai))
            {
                return true;
            }

            if (StationPatcher.PrefabsDefaultReject.Contains(info.name)
                || (StationPatcher.PatchedBuildingNames.Contains(info.name)
                    && (ai == null || !StationPatcher.IsNativeIntercityBusStation(info, ai))))
            {
                return false;
            }

            return true;
        }

        public static void Init()
        {
            if (_deployed)
            {
                return;
            }

            Load();
            SerializableDataExtension.instance.EventSaveData += OnSaveData;
            Singleton<BuildingManager>.instance.EventBuildingReleased += OnBuildingReleased;
            _deployed = true;
        }

        public static void Deinit()
        {
            if (!_deployed)
            {
                return;
            }

            DisabledBuildings.Clear();
            EnabledBuildings.Clear();
            SerializableDataExtension.instance.EventSaveData -= OnSaveData;
            Singleton<BuildingManager>.instance.EventBuildingReleased -= OnBuildingReleased;
            _deployed = false;
        }

        private static void OnBuildingReleased(ushort buildingId)
        {
            DisabledBuildings.Remove(buildingId);
            EnabledBuildings.Remove(buildingId);
        }

        private static void Load()
        {
            DisabledBuildings.Clear();
            EnabledBuildings.Clear();
            try
            {
                LoadSet(DataID, DisabledBuildings);
                LoadSet(DataIDEnabled, EnabledBuildings);
            }
            catch (Exception ex)
            {
                Utils.LogError($"IntercityBusControl: failed to load per-building acceptance data: {ex.Message}");
                DisabledBuildings.Clear();
                EnabledBuildings.Clear();
            }
        }

        private static void LoadSet(string id, HashSet<ushort> target)
        {
            var raw = SerializableDataExtension.instance.SerializableData.LoadData(id);
            if (raw == null || raw.Length < 4)
            {
                return;
            }

            var index = 0;
            var count = SerializableDataExtension.ReadInt32(raw, ref index);
            for (var i = 0; i < count; i++)
            {
                target.Add(SerializableDataExtension.ReadUInt16(raw, ref index));
            }
        }

        private static void OnSaveData()
        {
            try
            {
                SaveSet(DataID, DisabledBuildings);
                SaveSet(DataIDEnabled, EnabledBuildings);
            }
            catch (Exception ex)
            {
                Utils.LogError($"IntercityBusControl: failed to save per-building acceptance data: {ex.Message}");
            }
        }

        private static void SaveSet(string id, HashSet<ushort> source)
        {
            var data = new FastList<byte>();
            SerializableDataExtension.WriteInt32(source.Count, data);
            foreach (var buildingId in source)
            {
                SerializableDataExtension.WriteUInt16(buildingId, data);
            }

            SerializableDataExtension.instance.SerializableData.SaveData(id, data.ToArray());
        }
    }
}
