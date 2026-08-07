using System.Collections.Generic;
using UnityEngine;
using ImprovedPublicTransport.Util;

namespace ExpressBusServices
{
    public class ServiceBalancerUtil
    {
        private static Dictionary<ushort, ushort> redeploymentInstructions = new Dictionary<ushort, ushort>();

        private static Dictionary<ushort, ushort> vehicleCurrentlyAtStop = new Dictionary<ushort, ushort>();

        private static Dictionary<ushort, bool> redeploymentToTerminus= new Dictionary<ushort, bool>();

        private static readonly int STANDARD_BUS_PAX_THRESHOLD = 30;

        // AnalyzeTransportLinePopularity does a citizen-grid scan per stop — very expensive.
        // Cache per line for a short window so many buses hitting the same terminus don't
        // each pay full cost in the same second (FPS cliff 40→20).
        private const float AnalysisCacheSeconds = 2.5f;
        private static ushort _cachedAnalysisLineId;
        private static ushort _cachedAnalysisStartStop;
        private static float _cachedAnalysisRealtime;
        private static List<TransportLineSegmentAnalysis> _cachedAnalysisList;

        private struct TransportLineSegmentAnalysis
        {
            public ushort leadingTerminusStopId;
            public int stopCount;
            public int paxCount;

            public ushort mostWaitingPaxStopId;
            public int mostWaitingPaxCount;

            public bool segmentCanReceiveRedeployment;

            public TransportLineSegmentAnalysis(ushort leadingTerminusStopId, int stopCount, int paxCount, bool segmentCanReceiveRedeployment = false)
            {
                this.leadingTerminusStopId = leadingTerminusStopId;
                this.stopCount = stopCount;
                this.paxCount = paxCount;
                this.mostWaitingPaxStopId = 0;
                this.mostWaitingPaxCount = 0;
                this.segmentCanReceiveRedeployment = segmentCanReceiveRedeployment;
            }

            public void CompareAndUpdateMostWaitingStop(ushort stopID, int waitingPax)
            {
                if (waitingPax > mostWaitingPaxCount)
                {
                    mostWaitingPaxStopId = stopID;
                    mostWaitingPaxCount = waitingPax;
                }
            }
        }

        public static bool FindRedeployToTerminus(ushort vehicleID, ushort transportLineID, ushort currentTerminusStopId, out ushort terminusStopId)
        {
            terminusStopId = 0;
            if (EBSModConfig.CurrentExpressBusMode == EBSModConfig.ExpressMode.NONE
                || !EBSModConfig.UseServiceSelfBalancing)
            {
                // option not enabled; skip everything!
                MarkIsRedeployingToTerminus(vehicleID, false);
                return false;
            }
            if (!DepartureChecker.StopIsConsideredAsTerminus(currentTerminusStopId, transportLineID))
            {
                // not a terminus; check not allowed!
                MarkIsRedeployingToTerminus(vehicleID, false);
                return false;
            }
            List<TransportLineSegmentAnalysis> analysisList = GetCachedOrAnalyze(transportLineID, currentTerminusStopId);
            if (analysisList == null || analysisList.Count < 2)
            {
                // less than 2 segments, this means it is circular, and is not eligible for super-skipping
                MarkIsRedeployingToTerminus(vehicleID, false);
                return false;
            }
            // calculate the average number of pax waiting so that can determine the odds
            TransportLineSegmentAnalysis selfAnalysis = analysisList[0];
            float selfAvePax = selfAnalysis.paxCount * 1.0f / selfAnalysis.stopCount;
            List<float> otherAvePaxList = new List<float>(analysisList.Count);
            List<TransportLineSegmentAnalysis> acceptedList = new List<TransportLineSegmentAnalysis>(analysisList.Count);
            bool skipNext = true;
            float summedTotal = 0;
            foreach (TransportLineSegmentAnalysis analysis in analysisList)
            {
                if (skipNext)
                {
                    // this is just to skip the 1st stop
                    skipNext = false;
                    continue;
                }
                if (!analysis.segmentCanReceiveRedeployment)
                {
                    // non-self segment and cannot receive redeployment
                    // we only want to see segments that can receive redeployment
                    continue;
                }
                float avePax = analysis.paxCount * 1.0f / analysis.stopCount;
                otherAvePaxList.Add(avePax);
                acceptedList.Add(analysis);
                summedTotal += avePax;
            }
            if (OddsPermitRedeployment(selfAvePax, otherAvePaxList, vehicleID))
            {
                // check against the many segments, and determine which one to go to
                float nextInRangeRandNum = UnityEngine.Random.Range(0, summedTotal);
                float currentCapValue = 0;
                ushort loopingTerminusStopId = 0;
                ushort loopingMiddleStopId = 0;
                int loopingMiddleStopPaxCount = 0;
                skipNext = true;
                bool isRedeployingToTerminus = false;
                for (int i = 0; i < acceptedList.Count; i++)
                {
                    TransportLineSegmentAnalysis analysis = acceptedList[i];
                    loopingTerminusStopId = analysis.leadingTerminusStopId;
                    loopingMiddleStopId = analysis.mostWaitingPaxStopId;
                    loopingMiddleStopPaxCount = analysis.mostWaitingPaxCount;
                    currentCapValue += otherAvePaxList[i];
                    if (nextInRangeRandNum < currentCapValue)
                    {
                        // in this current segment
                        break;
                    }
                }
                // pick random 50% chance that it will go to a middle stop with the most passengers
                if (EBSModConfig.ServiceSelfBalancingCanDoMiddleStop && UnityEngine.Random.value < 0.5f && loopingMiddleStopPaxCount > STANDARD_BUS_PAX_THRESHOLD)
                {
                    // must have enough pax waiting
                    // deploy to middle bus stop
                    terminusStopId = loopingMiddleStopId;
                }
                else
                {
                    // deploy to terminus
                    terminusStopId = loopingTerminusStopId;
                    isRedeployingToTerminus = true;
                }
                // Utils.Log("EBS determines that a bus needs to be redeployed: " + currentTerminusStopId + " -> " + terminusStopId);
                MarkIsRedeployingToTerminus(vehicleID, isRedeployingToTerminus);
                return true;
            }
            MarkIsRedeployingToTerminus(vehicleID, false);
            return false;
        }

        private static List<TransportLineSegmentAnalysis> GetCachedOrAnalyze(ushort transportLineID, ushort startingTerminusStopId)
        {
            float now = Time.realtimeSinceStartup;
            if (_cachedAnalysisList != null
                && _cachedAnalysisLineId == transportLineID
                && _cachedAnalysisStartStop == startingTerminusStopId
                && (now - _cachedAnalysisRealtime) < AnalysisCacheSeconds)
            {
                return _cachedAnalysisList;
            }

            var list = AnalyzeTransportLinePopularity(transportLineID, startingTerminusStopId);
            _cachedAnalysisLineId = transportLineID;
            _cachedAnalysisStartStop = startingTerminusStopId;
            _cachedAnalysisRealtime = now;
            _cachedAnalysisList = list;
            return list;
        }

        private static List<TransportLineSegmentAnalysis> AnalyzeTransportLinePopularity(ushort transportLineID, ushort startingTerminusStopId)
        {
            // checks segment terminus -> segment total waiting pax
            // the first item of the list is guaranteed to be the "current segment"
            ushort loopingStopID = startingTerminusStopId;
            Dictionary<ushort, int> paxCount = new Dictionary<ushort, int>();
            Dictionary<ushort, bool> terminusCheck = new Dictionary<ushort, bool>();
            Dictionary<ushort, ushort> nextStopLink = new Dictionary<ushort, ushort>();
            while (true)
            {
                if (paxCount.ContainsKey(loopingStopID))
                {
                    // we looped back to the start
                    break;
                }

                // check waiting passengers
                int residentsWaiting, touristsWaiting;
                TransportLineUtil.CountPassengersWaiting(loopingStopID, out residentsWaiting, out touristsWaiting);
                paxCount.Add(loopingStopID, residentsWaiting + touristsWaiting);

                // check is terminus
                bool isTerminus = DepartureChecker.StopIsConsideredAsTerminus(loopingStopID, transportLineID);
                if (isTerminus)
                {
                    terminusCheck.Add(loopingStopID, isTerminus);
                }

                // next stop
                ushort nextStopId = TransportLine.GetNextStop(loopingStopID);
                if (nextStopId == 0 || nextStopId == loopingStopID)
                {
                    // Broken or open stop chain — abandon analysis rather than spin / index-OOB.
                    break;
                }
                nextStopLink.Add(loopingStopID, nextStopId);
                loopingStopID = nextStopId;

                // Utils.Log("Analyze iterating loop.");
            }

            // Broken/open chains never closed the first loop → paxCount/nextStopLink incomplete.
            // Guard before indexing to avoid KeyNotFoundException on the sim thread.
            if (paxCount.Count < 2
                || !paxCount.ContainsKey(startingTerminusStopId)
                || !nextStopLink.ContainsKey(startingTerminusStopId))
            {
                return new List<TransportLineSegmentAnalysis>();
            }

            // all information obtained; we are at the first stop of the line
            // create the list
            List<TransportLineSegmentAnalysis> analysisList = new List<TransportLineSegmentAnalysis>();
            int startPax = paxCount[startingTerminusStopId];
            TransportLineSegmentAnalysis analysis = new TransportLineSegmentAnalysis(startingTerminusStopId, 1, startPax, startPax > STANDARD_BUS_PAX_THRESHOLD);
            analysis.CompareAndUpdateMostWaitingStop(startingTerminusStopId, startPax);
            loopingStopID = nextStopLink[startingTerminusStopId];
            var groupGuard = 0;
            while (true)
            {
                if (terminusCheck.ContainsKey(loopingStopID))
                {
                    analysisList.Add(analysis);
                    analysis = new TransportLineSegmentAnalysis(loopingStopID, 0, 0, false);
                }
                if (loopingStopID == startingTerminusStopId)
                {
                    // we got to the start again
                    break;
                }

                if (!paxCount.TryGetValue(loopingStopID, out var stopPax)
                    || !nextStopLink.TryGetValue(loopingStopID, out var nextId))
                {
                    // Incomplete graph — drop partial analysis rather than throw.
                    return new List<TransportLineSegmentAnalysis>();
                }

                // add info
                analysis.stopCount++;
                analysis.paxCount += stopPax;
                analysis.CompareAndUpdateMostWaitingStop(loopingStopID, stopPax);
                analysis.segmentCanReceiveRedeployment |= stopPax > STANDARD_BUS_PAX_THRESHOLD;
                // move to next
                loopingStopID = nextId;
                if (++groupGuard > 32768)
                {
                    return new List<TransportLineSegmentAnalysis>();
                }
            }

            // return the list
            return analysisList;
        }

        internal static bool OddsPermitRedeployment(float selfAvePaxCount, List<float> otherAvePaxCountList, ushort vehicleID)
        {
            // using the analysis result, performs calculation and determines whether redeployment is allowed
            if (otherAvePaxCountList.Count == 0)
            {
                // no one to transfer to
                return false;
            }
            float properSelfValue = selfAvePaxCount * otherAvePaxCountList.Count;
            float properOtherValue = 0;
            for (int i = 0; i < otherAvePaxCountList.Count; i++)
            {
                properOtherValue += otherAvePaxCountList[i];
            }
            if (properOtherValue < properSelfValue)
            {
                // generally a better idea to stay at the current segment
                return false;
            }
            if (selfAvePaxCount == 0)
            {
                // to avoid div0 and because of sensibility, we will permit this
                // Utils.Log("Redeployment true probability (hard) " + 0 + " -> " + 999);
                return true;
            }
            // the odds of moving to any of the candidate segments
            float oddsMove = properOtherValue / properSelfValue;
            // we need to convert a exponential [0, inf) to a logistical [0, 1)
            // we will use a simple exponential fraction function to convert things
            // and the converted value can be directly used for RNG
            float probability = 1 - 1 / (Mathf.Pow(2, oddsMove - 1));
            // Utils.Log("Redeployment probability " + oddsMove + " -> " + probability);
            if (probability < 0)
            {
                return false;
            }
            // finally, do a RNG with such probability * the global config prob value
            // todo read from a config, or not
            float globalBalancerProbability = 0.5f;
            float theProbability = probability * globalBalancerProbability;
            // further reduce probability of repeated redeployment
            if (VehicleIsRedeployingToTerminus(vehicleID))
            {
                theProbability *= 0.5f;
            }
            // Random.value gives a PseudoUniform(0, 1) random value
            float rngPick = UnityEngine.Random.value;
            // Utils.Log("Redeployment true probability " + rngPick + " -> " + theProbability);
            return rngPick <= theProbability;
        }

        public static void MarkRedeployToNewTerminus(ushort vehicleID, ushort targetStopId)
        {
            if (EBSModConfig.CurrentExpressBusMode == EBSModConfig.ExpressMode.NONE)
            {
                ForgetVehicle(vehicleID);
                return;
            }
            // Never store an invalid stop — StartPathFind would IndexOutOfRange on it.
            if (targetStopId == 0 || !TransportStopSafety.IsLiveStopNode(targetStopId))
            {
                return;
            }
            // mark it here, so that later we can correctly apply this
            redeploymentInstructions[vehicleID] = targetStopId;
        }

        public static bool ReadRedeploymentInstructions(ushort vehicleID, out ushort redeploymentTarget, bool removeEntry = false)
        {
            redeploymentTarget = 0;
            if (EBSModConfig.CurrentExpressBusMode == EBSModConfig.ExpressMode.NONE)
            {
                redeploymentInstructions.Remove(vehicleID);
                MarkIsRedeployingToTerminus(vehicleID, false);
                return false;
            }
            if (!redeploymentInstructions.ContainsKey(vehicleID))
            {
                // no instructions
                return false;
            }
            redeploymentTarget = redeploymentInstructions[vehicleID];
            if (removeEntry)
            {
                redeploymentInstructions.Remove(vehicleID);
            }
            return true;
        }

        public static void MarkVehicleIsAtStopId(ushort vehicleID, ushort stopId)
        {
            vehicleCurrentlyAtStop[vehicleID] = stopId;
        }

        public static void MarkIsRedeployingToTerminus(ushort vehicleID, bool flag)
        {
            redeploymentToTerminus[vehicleID] = flag;
        }

        public static bool VehicleIsRedeployingToTerminus(ushort vehicleID)
        {
            if (!redeploymentToTerminus.ContainsKey(vehicleID))
            {
                return false;
            }
            return redeploymentToTerminus[vehicleID];
        }

        public static bool ReadVehicleCurrentlyAtWhatStop(ushort vehicleID, out ushort stopId)
        {
            stopId = 0;
            if (!vehicleCurrentlyAtStop.ContainsKey(vehicleID))
            {
                return false;
            }
            stopId = vehicleCurrentlyAtStop[vehicleID];
            return true;
        }

        public static void EnsureTableExists()
        {
            // reset the dictionary or whatever data struct we decided to use
            if (redeploymentInstructions == null)
            {
                redeploymentInstructions = new Dictionary<ushort, ushort>();
            }
            if (vehicleCurrentlyAtStop == null)
            {
                vehicleCurrentlyAtStop = new Dictionary<ushort, ushort>();
            }
            if (redeploymentToTerminus == null)
            {
                redeploymentToTerminus = new Dictionary<ushort, bool>();
            }
        }

        public static void ResetRedeploymentRecords()
        {
            // reset the dictionary or whatever data struct we decided to use
            redeploymentInstructions.Clear();
            vehicleCurrentlyAtStop.Clear();
            redeploymentToTerminus.Clear();
            _cachedAnalysisList = null;
            _cachedAnalysisLineId = 0;
            _cachedAnalysisStartStop = 0;
            _cachedAnalysisRealtime = 0f;
        }

        /// <summary>
        /// Drops per-vehicle redeploy bookkeeping when a vehicle despawns so a recycled
        /// vehicleID cannot inherit stale skip/redeploy state.
        /// </summary>
        public static void ForgetVehicle(ushort vehicleID)
        {
            if (redeploymentInstructions != null)
            {
                redeploymentInstructions.Remove(vehicleID);
            }
            if (vehicleCurrentlyAtStop != null)
            {
                vehicleCurrentlyAtStop.Remove(vehicleID);
            }
            if (redeploymentToTerminus != null)
            {
                redeploymentToTerminus.Remove(vehicleID);
            }
        }
    }
}
