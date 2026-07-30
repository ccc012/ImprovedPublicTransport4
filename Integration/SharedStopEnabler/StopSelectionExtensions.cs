// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
namespace SharedStopEnabler
{
    internal static class StopSelectionExtensions
    {
        /// <summary>
        /// Bus, tram and trolleybus are the transport types this reduced port supports sharing a
        /// road segment between. (Sightseeing/tourist bus lines use TransportInfo.TransportType.Bus
        /// too, so they are already covered without a separate case.)
        /// </summary>
        public static bool IsSharedStopTransport(this VehicleInfo.VehicleType vehicleType)
        {
            return vehicleType == VehicleInfo.VehicleType.Car
                   || vehicleType == VehicleInfo.VehicleType.Tram
                   || vehicleType == VehicleInfo.VehicleType.Trolleybus;
        }
    }
}
