using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.UI;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.PublicTransportStopButtonPatches
{
    public class OnMouseDownPatch
    {

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportStopButton), "OnMouseDown"),
                new PatchUtil.MethodDefinition(typeof(OnMouseDownPatch),
                    nameof(Prefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportStopButton), "OnMouseDown")
            );
        }
        
        private static bool Prefix(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is not UIButton button || button.objectUserData is not ushort objectUserData)
            {
                return true;
            }

            var nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
            if (objectUserData >= nodeBuffer.Length)
            {
                return true;
            }

            var position = nodeBuffer[objectUserData].m_position;
            var instanceID = InstanceID.Empty;
            instanceID.NetNode = objectUserData;
            if (PublicTransportStopButton.cameraController != null)
                //begin mod: zoom on shift pressed
                ToolsModifierControl.cameraController.SetTarget(instanceID, position, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                //end mod

            PublicTransportWorldInfoPanel.ResetScrollPosition();
            UIView.SetFocus(null);


            //begin mod: show PublicTransportStopWorldInfoPanel
            if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
            {
                PublicTransportStopWorldInfoPanel.instance.Show(position, instanceID);
                CommuterDestination.CommuterDestinationOverlay.SelectStop(objectUserData);
            }
            else
            {
                CommuterDestination.CommuterDestinationOverlay.Clear();
            }
            //end mod

            return false;
        }
    }
}
