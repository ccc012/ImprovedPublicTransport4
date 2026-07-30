// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
using System;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace SharedStopEnabler
{
    /// <summary>
    /// Placing a node (e.g. an intersection) on top of an existing road segment splits that
    /// segment into two, and the original segment ID no longer exists. Keeps the shared-stop
    /// registry pointed at the correct (new) segment ID instead of silently going stale after the
    /// player edits a road that had a shared stop on it.
    /// </summary>
    [HarmonyPatch(typeof(NetTool), "CreateNode", new[]
    {
        typeof(NetInfo), typeof(NetTool.ControlPoint), typeof(NetTool.ControlPoint), typeof(NetTool.ControlPoint),
        typeof(FastList<NetTool.NodePosition>), typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
        typeof(bool), typeof(ushort), typeof(ushort), typeof(ushort), typeof(int), typeof(int)
    }, new[]
    {
        ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
        ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
        ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out
    })]
    internal static class Patch_NetTool_CreateNode
    {
        [HarmonyPostfix]
        public static void Postfix(NetTool.ControlPoint middlePoint, ushort segment)
        {
            try
            {
                if (!ModSetting.Instance.EnableSharedStopEnabler || middlePoint.m_segment == 0 || segment == 0)
                {
                    return;
                }

                if (SharedStopRegistry.IsSharedStopSegment(middlePoint.m_segment))
                {
                    SharedStopRegistry.RenameSegment(middlePoint.m_segment, segment);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"SharedStopEnabler: failed in NetTool.CreateNode postfix: {ex.Message}");
            }
        }
    }
}
