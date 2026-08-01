// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.SimHelper
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using ColossalFramework;
using UnityEngine;

namespace ImprovedPublicTransport
{
    public class SimHelper : MonoBehaviour
    {
        private static float _simulationTime;

        // Was static Awake() - Unity never invokes static lifecycle methods on MonoBehaviours,
        // so SimulationTime never reset on attach (only on destroy). Instance Awake is required.
        private void Awake()
        {
            _simulationTime = 0f;
        }

        public static float SimulationTime => _simulationTime;

        private void Update()
        {
            if (!Singleton<SimulationManager>.exists)
            {
                return;
            }

            var sim = Singleton<SimulationManager>.instance;
            if (sim.SimulationPaused)
            {
                return;
            }

            _simulationTime += sim.m_simulationTimeDelta;
        }

        private void OnDestroy()
        {
            _simulationTime = 0f;
        }
    }
}