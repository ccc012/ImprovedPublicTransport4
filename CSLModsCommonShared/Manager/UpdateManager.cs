using CSLModsCommon.Serialization;
using ICities;
using System;
using System.Collections.Generic;

namespace CSLModsCommon.Manager; 
public class UpdateManager : ManagerBase {
    private readonly object _lookupLock = new();
    private Dictionary<Type, ManagerBase> _simulationPhaseManagers;
    private Dictionary<Type, ManagerBase> _serializationPhaseManagers;
    private Dictionary<Type, ISimulation> _simulationInterfaces;
    private Dictionary<Type, ISerializable> _serializationInterfaces;
    private SerializationManager _serializationManager;

    protected override void OnCreate() {
        base.OnCreate();
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
        foreach (var serializationLookupValue in Snapshot(_serializationInterfaces)) _serializationManager.SerializeData(serializationLookupValue);
    }

    public void InvokeDeserialize() {
        foreach (var serializationLookupValue in Snapshot(_serializationInterfaces)) _serializationManager.DeserializeData(serializationLookupValue);
    }

    public void InvokeBindThreadingContext(IThreading threading) {
        foreach (var value in Snapshot(_simulationInterfaces)) value.OnBindThreadingContext(threading);
    }

    public void InvokePreSimulationTick() {
        foreach (var value in Snapshot(_simulationInterfaces)) value.OnPreSimulationTick();
    }

    public void InvokePreSimulationFrame() {
        foreach (var value in Snapshot(_simulationInterfaces)) value.OnPreSimulationFrame();
    }

    public void InvokePostSimulationFrame() {
        foreach (var value in Snapshot(_simulationInterfaces)) value.OnPostSimulationFrame();
    }

    public void InvokePostSimulationTick() {
        foreach (var value in Snapshot(_simulationInterfaces)) value.OnPostSimulationTick();
    }

    public void InvokeThreadingUpdate(float realTimeDelta, float simulationTimeDelta) {
        foreach (var value in Snapshot(_simulationInterfaces)) value.OnThreadingUpdate(realTimeDelta, simulationTimeDelta);
    }

    public void InvokeUnbindThreadingContext() {
        foreach (var value in Snapshot(_simulationInterfaces)) value.OnUnbindThreadingContext();
    }

    private void AddToLookup(Dictionary<Type, ManagerBase> lookup, Type type) {
        if (Domain.TryGetManager(type, out var manager)) {
            lock (_lookupLock) {
                if (lookup.ContainsKey(type)) return;
                lookup[type] = manager;
                RegisterInterfaces(manager);
            }
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

    private T[] Snapshot<T>(Dictionary<Type, T> lookup) {
        lock (_lookupLock) {
            var values = new T[lookup.Count];
            lookup.Values.CopyTo(values, 0);
            return values;
        }
    }
}
