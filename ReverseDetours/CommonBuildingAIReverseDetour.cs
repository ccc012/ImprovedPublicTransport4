using System.Runtime.CompilerServices;
using ImprovedPublicTransport.RedirectionFramework.Attributes;

namespace ImprovedPublicTransport.ReverseDetours
{
    //TODO: Only to access the private method. use a reversed patch instead
    [TargetType(typeof(CommonBuildingAI))]
    public class CommonBuildingAIReverseDetour : ShelterAI
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [RedirectReverse]
        public static void CalculateOwnVehicles(CommonBuildingAI ai, ushort buildingID, ref Building data,
            TransferManager.TransferReason material, ref int count, ref int cargo, ref int capacity, ref int outside)
        {
            // RedirectReverse body — replaced at deploy with the real private method.
            // Never log here (would spam every depot capacity query if redirect failed).
        }
    }
}