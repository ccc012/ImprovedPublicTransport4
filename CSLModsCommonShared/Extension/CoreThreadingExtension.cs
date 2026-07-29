using CSLModsCommon.Logging;
using CSLModsCommon.Manager;
using ICities;

namespace CSLModsCommon.Extension; 
public class CoreThreadingExtension : IThreadingExtension {
    private ILog _logger;
    private UpdateManager _updateManager;

    public void OnCreated(IThreading threading) {
        _logger = LogManager.GetLogger();
        _logger.Info("CoreThreadingExtension OnCreated");
        _updateManager = Domain.DefaultDomain.GetOrCreateManager<UpdateManager>();
        _updateManager.InvokeBindThreadingContext(threading);
    }

    public void OnUpdate(float realTimeDelta, float simulationTimeDelta) => _updateManager.InvokeThreadingUpdate(realTimeDelta, simulationTimeDelta);

    public void OnBeforeSimulationTick() => _updateManager.InvokePreSimulationTick();

    public void OnBeforeSimulationFrame() => _updateManager.InvokePreSimulationFrame();

    public void OnAfterSimulationFrame() => _updateManager.InvokePostSimulationFrame();

    public void OnAfterSimulationTick() => _updateManager.InvokePostSimulationTick();

    public void OnReleased() {
        _logger.Info("CoreThreadingExtension OnReleased");
        _updateManager.InvokeUnbindThreadingContext();
    }
}