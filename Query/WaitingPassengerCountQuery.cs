using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace ImprovedPublicTransport.Query
{
    public static class WaitingPassengerCountQuery
    {
        private const float CacheSeconds = 0.5f;
        private const int CacheLimit = 1024;
        private static readonly Dictionary<ushort, CacheEntry> Cache = new Dictionary<ushort, CacheEntry>();

        public static void ClearCache()
        {
            Cache.Clear();
        }

        public static int Query(ushort currentStop, out ushort nextStop, out byte max)
        {
            var now = Time.unscaledTime;
            CacheEntry cached;
            if (Cache.TryGetValue(currentStop, out cached) && now - cached.Time < CacheSeconds)
            {
                nextStop = cached.NextStop;
                max = cached.Max;
                return cached.Count;
            }

            nextStop = TransportLine.GetNextStop(currentStop);
            max = 0;
            if (currentStop == 0 || nextStop == 0)
            {
                return 0;
            }

            var instance1 = Singleton<CitizenManager>.instance;
            var instance2 = Singleton<NetManager>.instance;
            var position1 = instance2.m_nodes.m_buffer[currentStop].m_position;
            var position2 = instance2.m_nodes.m_buffer[nextStop].m_position;
            var num1 = Mathf.Max((int)((position1.x - 64.0) / 8.0 + 1080.0), 0);
            var num2 = Mathf.Max((int)((position1.z - 64.0) / 8.0 + 1080.0), 0);
            var num3 = Mathf.Min((int)((position1.x + 64.0) / 8.0 + 1080.0), 2159);
            var num4 = Mathf.Min((int)((position1.z + 64.0) / 8.0 + 1080.0), 2159);
            var num6 = 0;
            for (var index1 = num2; index1 <= num4; ++index1)
            {
                for (var index2 = num1; index2 <= num3; ++index2)
                {
                    var instanceID = instance1.m_citizenGrid[index1 * 2160 + index2];
                    var num7 = 0;
                    while (instanceID != 0)
                    {
                        int nextGridInstance = instance1.m_instances.m_buffer[instanceID].m_nextGridInstance;
                        ref var citizenInstance = ref instance1.m_instances.m_buffer[instanceID];
                        if ((citizenInstance.m_flags & CitizenInstance.Flags.WaitingTransport) != 0)
                        {
                            if (citizenInstance.Info?.m_citizenAI != null
                                && Vector3.SqrMagnitude((Vector3)citizenInstance.m_targetPos - position1) < 4096.0
                                && citizenInstance.Info.m_citizenAI.TransportArriveAtSource(
                                    instanceID, ref citizenInstance, position1, position2))
                            {
                                var waitCounter = citizenInstance.m_waitCounter;
                                max = Math.Max(waitCounter, max);
                                ++num6;
                            }
                        }

                        instanceID = (ushort)nextGridInstance;
                        if (++num7 > instance1.m_instances.m_buffer.Length)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core,
                                "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            if (Cache.Count >= CacheLimit && !Cache.ContainsKey(currentStop))
            {
                Cache.Clear();
            }

            Cache[currentStop] = new CacheEntry(num6, nextStop, max, now);
            return num6;
        }

        private struct CacheEntry
        {
            internal readonly int Count;
            internal readonly ushort NextStop;
            internal readonly byte Max;
            internal readonly float Time;

            internal CacheEntry(int count, ushort nextStop, byte max, float time)
            {
                Count = count;
                NextStop = nextStop;
                Max = max;
                Time = time;
            }
        }
    }
}