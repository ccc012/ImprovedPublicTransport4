using CSLModsCommon.Serialization;
using ICities;
using System;
using System.Collections.Generic;

namespace CSLModsCommon.Manager; 
public class UpdateManager : ManagerBase {
    private Dictionary<Type, ManagerBase> _managerLookup;
    private Dictionary<Type, ManagerBase> _simulationPhaseManagers;
    private Dictionary<Type, ManagerBase> _serializationPhaseManagers;
    private Dictionary<Type, ISimulation> _simulationInterfaces;
    private Dictionary<Type, ISerializable> _serializationInterfaces;
    private SerializationManager _serializationManager;

    protected override void OnCreate() {
        base.OnCreate();
        _managerLookup = Domain.ManagerLookup;
        _simulationPhaseManagers = new Dictionary<Type, ManagerBase>();
        _serializationPhaseManagers = new Dictionary<Type, ManagerBase>();
        _simulationInterfaces = new Dictionary<Type, ISimulation>();
        _serializationInterfaces = new Dictionary<Type, ISerializable>();
        _serializationManager = Domain.GetOrCreateManager<SerializationManager>();
    }

    public T UpdateAt<T>(UpdatePhase updatePhase) where T : ManagerBase, new() {
        var manager = Domain.GetOrCreateManager<T>();
        switch (updatePhase) {
            case UpdatePhase.Simulation:
                AddToLookup(_simulationPhaseManagers, typeof(T));
                break;
            case UpdatePhase.Serialize:
                AddToLookup(_serializationPhaseManagers, typeof(T));
                break;
            case UpdatePhase.Default:
            default:
                break;
        }

        return manager;
    }

    public void InvokeSerialize() {
        foreach (var serializationLookupValue in _serializationInterfaces.Values) _serializationManager.SerializeData(serializationLookupValue);
    }

    public void InvokeDeserialize() {
        foreach (var serializationLookupValue in _serializationInterfaces.Values) _serializationManager.DeserializeData(serializationLookupValue);
    }

    public void InvokeBindThreadingContext(IThreading threading) {
        foreach (var value in _simulationInterfaces.Values) value.OnBindThreadingContext(threading);
    }

    public void InvokePreSimulationTick() {
        foreach (var value in _simulationInterfaces.Values) value.OnPreSimulationTick();
    }

    public void InvokePreSimulationFrame() {
        foreach (var value in _simulationInterfaces.Values) value.OnPreSimulationFrame();
    }

    public void InvokePostSimulationFrame() {
        foreach (var value in _simulationInterfaces.Values) value.OnPostSimulationFrame();
    }

    public void InvokePostSimulationTick() {
        foreach (var value in _simulationInterfaces.Values) value.OnPostSimulationTick();
    }

    public void InvokeThreadingUpdate(float realTimeDelta, float simulationTimeDelta) {
        foreach (var value in _simulationInterfaces.Values) value.OnThreadingUpdate(realTimeDelta, simulationTimeDelta);
    }

    public void InvokeUnbindThreadingContext() {
        foreach (var value in _simulationInterfaces.Values) value.OnUnbindThreadingContext();
    }

    private void AddToLookup(Dictionary<Type, ManagerBase> lookup, Type type) {
        if (_managerLookup.TryGetValue(type, out var manager) && !lookup.ContainsKey(type)) {
            lookup[type] = manager;
            RegisterInterfaces(manager);
        }
    }

    private void RegisterInterfaces(ManagerBase manager) {
        if (manager is ISimulation simulationManager) {
            var type = manager.GetType();
            if (!_simulationInterfaces.ContainsKey(type)) {
                _simulationInterfaces[type] = simulationManager;
                Logger.Debug($"UpdateManager: Registered {type} as ISimulation");
            }
        }

        if (manager is ISerializable serializeManager) {
            var type = manager.GetType();
            if (!_serializationInterfaces.ContainsKey(type)) {
                _serializationInterfaces[type] = serializeManager;
                Logger.Debug($"UpdateManager: Registered {type} as ISerializable");
            }
        }
    }
}