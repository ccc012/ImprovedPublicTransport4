using CSLModsCommon.Logging;
using CSLModsCommon.Manager;
using ICities;

namespace CSLModsCommon.Extension; 
public class CoreSerializableDataExtension : ISerializableDataExtension {
    private ILog _logger;
    private UpdateManager _updateManager;

    public void OnCreated(ISerializableData serializableData) {
        _logger = LogManager.GetLogger();
        _logger.Info("CoreSerializableDataExtension OnCreated");
        _updateManager = Domain.DefaultDomain.GetOrCreateManager<UpdateManager>();
    }

    public void OnLoadData() {
        _logger.Info("CoreSerializableDataExtension OnLoadData");
        _updateManager.InvokeDeserialize();
    }

    public void OnSaveData() {
        _logger.Info("CoreSerializableDataExtension OnSaveData");
        _updateManager.InvokeSerialize();
    }

    public void OnReleased() => _logger.Info("CoreSerializableDataExtension OnReleased");
}