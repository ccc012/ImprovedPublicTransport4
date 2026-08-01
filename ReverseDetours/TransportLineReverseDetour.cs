using System.Runtime.CompilerServices;
using ImprovedPublicTransport.RedirectionFramework.Attributes;

namespace ImprovedPublicTransport.ReverseDetours
{
    //TODO: Only to access the private method. use a reversed patch instead
    [TargetType(typeof(TransportLine))]
    public struct TransportLineReverseDetour
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [RedirectReverse]
        public static ushort GetActiveVehicle(ref TransportLine thisLine, int index)
        {
            // RedirectReverse body — replaced at deploy with the real private method.
            // Never Debug.Log here (would spam every call if redirect failed).
            return 0;
        }
    }
}