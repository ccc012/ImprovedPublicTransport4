using ICities;

namespace CSLModsCommon;

public interface ISimulation {
    void OnBindThreadingContext(IThreading threading);
    void OnUnbindThreadingContext();
    void OnThreadingUpdate(float realTimeDelta, float simulationTimeDelta);
    void OnPreSimulationTick();
    void OnPreSimulationFrame();
    void OnPostSimulationFrame();
    void OnPostSimulationTick();
}