using HarmonyLib;
using UnityEngine;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using ImprovedPublicTransport;

namespace BetterBusStopPosition
{

[HarmonyPatch(typeof(BusAI))]
public static class BusAI_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch("CalculateSegmentPosition")]
    public static void CalculateSegmentPosition_Postfix(BusAI __instance, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position position, uint laneID, byte offset, ref Vector3 pos, ref Vector3 dir)
    {
        // Only apply BBSP if enabled (not Disabled mode)
        var mode = ModSetting.Instance.BbspLogic;
        if(mode == ModSetting.BbspLogicModes.Disabled)
            return;

        // Only process if we're arriving/leaving (same condition as original method)
        if ((vehicleData.m_flags & (Vehicle.Flags.Leaving | Vehicle.Flags.Arriving)) == 0)
            return;

        NetManager instance = Singleton<NetManager>.instance;
        NetInfo info = instance.m_segments.m_buffer[position.m_segment].Info;
        if (info.m_lanes == null || info.m_lanes.Length <= position.m_lane)
            return;

        NetInfo.Lane laneInfo = info.m_lanes[position.m_lane];
        float stopOffset = laneInfo.m_stopOffset;
        NetSegment.Flags flags = NetSegment.Flags.None;
        
        if ((instance.m_segments.m_buffer[position.m_segment].m_flags & NetSegment.Flags.Invert) != 0)
        {
            stopOffset = 0f - stopOffset;
            flags = NetSegment.Flags.Invert;
        }

        float laneOffset = (float)(int)offset * 0.003921569f;
        CalculateModifiedStopPosition(ref instance.m_lanes.m_buffer[laneID], laneOffset, vehicleID, ref vehicleData, laneInfo, flags, stopOffset, out pos, out dir);
    }

    // Calculates modified bus stop position and direction based on selected BBSP logic mode.
    // This postfix receives results from vanilla CalculateSegmentPosition and may modify them
    // according to the active mode (unlike original BBSP which used a transpiler to bypass SmootherStep).
    //
    // The vanilla CalculateStopPositionAndDirection uses SmootherStep(0.5, 0, |laneOffset - 0.5|)
    // to fade lateral curb offset, peaking at 1.0 when laneOffset=0.5 (lane center).
    // Original BBSP moved the stop forward (targetOffset > 0.5), causing partial curb pull and visible swooping.
    //
    // Modes:
    // - Disabled: Pass through vanilla behavior unchanged
    // - OriginalLogic: Matches original BBSP logic by calculating modified offset and passing to vanilla method
    // - UpdatedLogic: Direct Bezier calculation with full curb displacement (disabled, for future testing)
    public static void CalculateModifiedStopPosition( ref NetLane lane, float laneOffset, ushort vehicleID,
        ref Vehicle vehicleData, NetInfo.Lane laneInfo, NetSegment.Flags flags,
        float stopOffset, out Vector3 pos, out Vector3 dir )
    {
        var mode = ModSetting.Instance.BbspLogic;

        if( mode == ModSetting.BbspLogicModes.Disabled )
        {
            // Vanilla behavior: use original offset as-is
            lane.CalculateStopPositionAndDirection( laneOffset, stopOffset, out pos, out dir );
            return;
        }

        if( mode == ModSetting.BbspLogicModes.OriginalLogic )
        {
            // Original BBSP logic: Calculate modified offset based on vehicle length and lane geometry,
            // then pass to vanilla CalculateStopPositionAndDirection (which applies SmootherStep tapering).
            // This produces mathematically equivalent results to the original transpiler approach.
            float modifiedOffset = laneOffset; // fallback: use vanilla offset if conditions not met
            float laneLength = lane.m_length;

            // Return vanilla offset if vehicle is leaving
            if( ( vehicleData.m_flags & Vehicle.Flags.Leaving ) == 0 && laneLength >= 1f )
            {
                float margin = laneLength / 6;
                float vehicleLength = vehicleData.Info.m_generatedInfo != null ? vehicleData.Info.m_generatedInfo.m_size.z : 0f;
                float newStopOffset = 1 - ( margin + vehicleLength / 2 ) / laneLength;

                if( newStopOffset >= 0.5f )
                {
                    bool inverted = ( laneInfo.m_finalDirection & NetInfo.Direction.Backward ) != 0;
                    if(( flags & NetSegment.Flags.Invert ) != 0 )
                        inverted = !inverted;

                    float adjusted = inverted ? ( 1f - laneOffset ) : laneOffset;
                    adjusted *= 2 * newStopOffset;
                    modifiedOffset = inverted ? ( 1f - adjusted ) : adjusted;
                }
            }

            // Use vanilla method with modified offset (applies SmootherStep curve to stopOffset)
            lane.CalculateStopPositionAndDirection( modifiedOffset, stopOffset, out pos, out dir );
            return;
        }

        // Mode == UpdatedLogic: Improved algorithm bypassing SmootherStep entirely (disabled, pending testing)
        // Calculates targetOffset as in OriginalLogic, but then directly accesses the Bezier curve
        // instead of calling vanilla method. Applies full curb displacement without SmootherStep tapering,
        // allowing smooth uninterrupted curb approach for better visual flow.
        //
        // float targetOffset = laneOffset;
        // float laneLength2 = lane.m_length;
        //
        // if( laneLength2 >= 1f && ( vehicleData.m_flags & Vehicle.Flags.Leaving ) == 0 )
        // {
        //     float margin = laneLength2 / 6;
        //     float vehicleLength = vehicleData.Info.m_generatedInfo != null ? vehicleData.Info.m_generatedInfo.m_size.z : 0f;
        //     float newStopOffset = 1 - ( margin + vehicleLength / 2 ) / laneLength2;
        //
        //     if( newStopOffset >= 0.5f )
        //     {
        //         bool inverted = ( laneInfo.m_finalDirection & NetInfo.Direction.Backward ) != 0;
        //         if(( flags & NetSegment.Flags.Invert ) != 0 )
        //             inverted = !inverted;
        //
        //         float adjusted = inverted ? ( 1f - laneOffset ) : laneOffset;
        //         adjusted *= 2 * newStopOffset;
        //         targetOffset = inverted ? ( 1f - adjusted ) : adjusted;
        //     }
        // }
        //
        // // Direct Bezier access (no vanilla SmootherStep curve)
        // pos = lane.m_bezier.Position( targetOffset );
        // dir = lane.m_bezier.Tangent( targetOffset );
        //
        // // Full curb displacement applied unconditionally at stop point
        // if( stopOffset != 0f )
        //     pos += Vector3.Cross( Vector3.up, dir ).normalized * stopOffset;

        // Fallback for any unrecognised mode value: vanilla behaviour
        lane.CalculateStopPositionAndDirection( laneOffset, stopOffset, out pos, out dir );
    }
}

[HarmonyPatch(typeof(TrolleybusAI))]
public static class TrolleybusAI_Patch
{
    [HarmonyPostfix]
    [HarmonyPatch("CalculateSegmentPosition")]
    public static void CalculateSegmentPosition_Postfix(TrolleybusAI __instance, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position position, uint laneID, byte offset, ref Vector3 pos, ref Vector3 dir)
    {
        // Same logic as BusAI - reuse the implementation
        var mode = ModSetting.Instance.BbspLogic;
        if(mode == ModSetting.BbspLogicModes.Disabled)
            return;

        if ((vehicleData.m_flags & (Vehicle.Flags.Leaving | Vehicle.Flags.Arriving)) == 0)
            return;

        NetManager instance = Singleton<NetManager>.instance;
        NetInfo info = instance.m_segments.m_buffer[position.m_segment].Info;
        if (info.m_lanes == null || info.m_lanes.Length <= position.m_lane)
            return;

        NetInfo.Lane laneInfo = info.m_lanes[position.m_lane];
        float stopOffset = laneInfo.m_stopOffset;
        NetSegment.Flags flags = NetSegment.Flags.None;
        
        if ((instance.m_segments.m_buffer[position.m_segment].m_flags & NetSegment.Flags.Invert) != 0)
        {
            stopOffset = 0f - stopOffset;
            flags = NetSegment.Flags.Invert;
        }

        float laneOffset = (float)(int)offset * 0.003921569f;
        BusAI_Patch.CalculateModifiedStopPosition(ref instance.m_lanes.m_buffer[laneID], laneOffset, vehicleID, ref vehicleData, laneInfo, flags, stopOffset, out pos, out dir);
    }
}

}
