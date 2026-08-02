using System.Text;
using UnityEngine;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Logs an unconditional (not gated behind Verbose) heartbeat line every ~15 real seconds while
    /// a level is loaded: frame count, whether the simulation is paused, and which integrations are
    /// currently enabled. Exists purely to narrow down freezes/hangs that happen minutes into a
    /// session with no exception - the timestamp of the LAST heartbeat before a hang is the last
    /// point IPT4's own main-thread Update loop is confirmed to still be alive, and the integration
    /// list rules whole categories of suspects in or out without needing per-integration
    /// instrumentation everywhere.
    ///
    /// Does not replace targeted diagnosis - if the freeze is on the simulation thread (a
    /// ThreadingExtensionBase.OnUpdate hang, e.g. AutoLineColor/ColorMonitor or StopsAndStations)
    /// rather than the main/render thread, this heartbeat (itself on the main thread) may keep
    /// ticking right up to the point the whole process locks up, since Unity's message pump can
    /// still be alive while the simulation thread is stuck. Still useful then: a heartbeat that
    /// keeps going past the moment input stops responding points squarely at the simulation thread
    /// instead of the render/UI thread, which is exactly the kind of thing "camera still moves but
    /// nothing is clickable" needs to distinguish.
    /// </summary>
    internal sealed class SessionWatchdog : MonoBehaviour
    {
        private const float IntervalSeconds = 15f;
        private float _nextBeatRealtime;

        private void Update()
        {
            var now = Time.unscaledTime;
            if (now < _nextBeatRealtime)
            {
                return;
            }

            _nextBeatRealtime = now + IntervalSeconds;

            try
            {
                Utils.Log(BuildHeartbeat());
            }
            catch
            {
                // A watchdog must never be the thing that throws.
            }
        }

        private static string BuildHeartbeat()
        {
            var sb = new StringBuilder(256);
            sb.Append("Heartbeat: frame=").Append(Time.frameCount);
            sb.Append(" unscaledTime=").Append(Time.unscaledTime.ToString("F0"));

            try
            {
                var sim = ColossalFramework.Singleton<SimulationManager>.exists
                    ? ColossalFramework.Singleton<SimulationManager>.instance
                    : null;
                sb.Append(" simPaused=").Append(sim != null ? sim.SimulationPaused.ToString() : "n/a");
            }
            catch
            {
                sb.Append(" simPaused=err");
            }

            try
            {
                var s = ModSetting.Instance;
                if (s != null)
                {
                    sb.Append(" active=[");
                    var first = true;
                    void Flag(string name, bool on)
                    {
                        if (!on)
                        {
                            return;
                        }

                        if (!first)
                        {
                            sb.Append(',');
                        }

                        sb.Append(name);
                        first = false;
                    }

                    Flag("AdvancedStopSelection", s.EnableAdvancedStopSelection);
                    Flag("BetterBoarding", s.EnableBetterBoarding);
                    Flag("MileageTaxi", s.EnableMileageTaxi);
                    Flag("ElevatedStops", s.EnableElevatedStops);
                    Flag("AutoLineBudget", s.AutoLineBudgetMode == ModSetting.AutoLineBudgetModes.Enabled);
                    Flag("TrainDisplay", s.TrainDisplayMode == ModSetting.TrainDisplayModes.Enabled);
                    Flag("TicketPriceCustomizer", s.TicketPriceCustomizerMode == ModSetting.TicketPriceCustomizerModes.Enabled);
                    Flag("OptimisedOutsideConnections", s.EnableOptimisedOutsideConnections);
                    Flag("UnlimitedOutsideConnections", s.EnableUnlimitedOutsideConnections);
                    Flag("SingleTrainTrackAI", s.EnableSingleTrainTrackAI);
                    Flag("StopStacker", s.EnableStopStacker);
                    Flag("PublicTransportUnstucker", s.EnablePublicTransportUnstucker);
                    Flag("IntercityBusControl", s.EnableIntercityBusControl);
                    Flag("FlightTracker", s.EnableFlightTracker);
                    Flag("SubBuildingsTabs", s.EnableSubBuildingsTabs);
                    Flag("TaxiStandFix", s.EnableTaxiStandFix);
                    Flag("SharedStopEnabler", s.EnableSharedStopEnabler);
                    Flag("CommuterDestination", s.EnableCommuterDestination);
                    Flag("AutoNameStops", s.EnableAutoNameStops);
                    Flag("RescueFullwidthDigits", s.EnableRescueFullwidthDigits);
                    // AutoLineColor/naming and StopsAndStations waiting-passenger limits have no single
                    // on/off flag (strategy enums / always-registered ThreadingExtensionBase) - noted
                    // separately since "active=[]" would otherwise wrongly read as "nothing running".
                    sb.Append("] colorStrategy=").Append(s.AutoLineColorColorStrategy);
                    sb.Append(" namingStrategy=").Append(s.AutoLineColorNamingStrategyMode);
                }
            }
            catch
            {
                sb.Append(" active=err");
            }

            return sb.ToString();
        }
    }
}
