using CSLModsCommon.Logging;
using CSLModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.Manager;

public sealed class Domain {
    private static readonly Dictionary<string, Domain> AllDomains = new();
    private static Domain _defaultDomain;
    public static Domain DefaultDomain => _defaultDomain ??= new Domain($"{AssemblyHelper.CurrentAssemblyName}DefaultDomain");
    private readonly Dictionary<Type, ManagerBase> _managerLookup;
    private readonly ILog _logger;
    private bool _isCachedModManager;

    public static event Action<Domain, ManagerBase> ManagerCreated;
    public static event Action<Domain, ManagerBase> ManagerDestroyed;

    public string Name { get; }
    public bool Disposed { get; private set; }
    internal Dictionary<Type, ManagerBase> ManagerLookup => _managerLookup;

    public Domain(string name) {
        _managerLookup = new Dictionary<Type, ManagerBase>();
        _logger = LogManager.GetLogger();
        Name = name;
        Disposed = false;
        if (AllDomains.ContainsKey(name)) _logger.Warn($"Domain with name '{name}' already exists. Overwriting.");

        AllDomains[name] = this;
    }

    public static Domain Get(string name) => AllDomains.TryGetValue(name, out var domain) ? domain : null;

    public static bool Remove(string name) => AllDomains.Remove(name);

    public static IEnumerable<Domain> ListAllDomains() => AllDomains.Values;

    public IEnumerable<Type> ListManagerTypes() => _managerLookup.Keys;

    public bool HasManager<T>() => _managerLookup.ContainsKey(typeof(T));

    public T GetManager<T>() where T : ManagerBase {
        if (_managerLookup.TryGetValue(typeof(T), out var manager))
            return (T)manager;
        return null;
    }

    public T GetOrCreateManager<T>() where T : ManagerBase, new() {
        if (_managerLookup.TryGetValue(typeof(T), out var manager))
            return (T)manager;

        var type = typeof(T);
        var instance = new T();
        try {
            _managerLookup[type] = instance;
            instance.OnInstanceCreated();
            ManagerCreated?.Invoke(this, instance);
            return instance;
        }
        catch (Exception ex) {
            _logger.Error(ex, $"Error creating manager {type.Name}");
            return null;
        }
    }

    public T GetModManager<T>() where T : ModManagerBase {
        if (_managerLookup.TryGetValue(typeof(T), out var manager))
            return (T)manager;
        return null;
    }

    public ModManagerBase GetModManager() => _isCachedModManager ? GetModManager<ModManagerBase>() : null;

    internal void CacheModManager<T>(T manager) where T : ModManagerBase {
        if (_isCachedModManager) return;
        if (manager is null) {
            _logger.Error("Object is null when caching mod manager");
            return;
        }
        
        var genericType = typeof(T);
        if (!_managerLookup.ContainsKey(genericType)) {
            _managerLookup.Add(genericType, manager);
            _logger.Verbose($"ModManagerBase cached: {genericType}");
        }
        
        _isCachedModManager = true;
    }

    public void DestroyManager<T>() where T : ManagerBase {
        if (!_managerLookup.TryGetValue(typeof(T), out var manager)) return;
        ManagerDestroyed?.Invoke(this, manager);
        manager.OnInstanceDestroy();
        _managerLookup.Remove(typeof(T));
    }

    public void DestroyAllManagers() {
        foreach (var manager in _managerLookup.Values)
            try {
                ManagerDestroyed?.Invoke(this, manager);
                manager.OnInstanceDestroy();
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error destroying {manager.GetType().Name}");
            }

        _managerLookup.Clear();
    }

    public IEnumerable<T> FilterManagers<T>(Func<T, bool> predicate) where T : ManagerBase => _managerLookup.Values.OfType<T>().Where(predicate);

    public override string ToString() => Name;

    public void Dispose() {
        if (Disposed) return;
        Disposed = true;
        DestroyAllManagers();
    }
}