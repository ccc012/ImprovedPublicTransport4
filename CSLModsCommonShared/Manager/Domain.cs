using CSLModsCommon.Logging;
using CSLModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.Manager;

public sealed class Domain {
    private static readonly Dictionary<string, Domain> AllDomains = new();
    private static readonly object AllDomainsLock = new();
    private static Domain _defaultDomain;
    public static Domain DefaultDomain {
        get {
            lock (AllDomainsLock)
                return _defaultDomain ??= new Domain($"{AssemblyHelper.CurrentAssemblyName}DefaultDomain");
        }
    }
    private readonly Dictionary<Type, ManagerBase> _managerLookup;
    private readonly object _managerLock = new();
    private readonly ILog _logger;
    private bool _isCachedModManager;

    public static event Action<Domain, ManagerBase> ManagerCreated;
    public static event Action<Domain, ManagerBase> ManagerDestroyed;

    public string Name { get; }
    public bool Disposed { get; private set; }

    public Domain(string name) {
        _managerLookup = new Dictionary<Type, ManagerBase>();
        _logger = LogManager.GetLogger();
        Name = name;
        Disposed = false;
        lock (AllDomainsLock) {
            if (AllDomains.ContainsKey(name)) _logger.Warn($"Domain with name '{name}' already exists. Overwriting.");
            AllDomains[name] = this;
        }
    }

    public static Domain Get(string name) {
        lock (AllDomainsLock)
            return AllDomains.TryGetValue(name, out var domain) ? domain : null;
    }

    public static bool Remove(string name) {
        lock (AllDomainsLock)
            return AllDomains.Remove(name);
    }

    public static IEnumerable<Domain> ListAllDomains() {
        lock (AllDomainsLock)
            return AllDomains.Values.ToArray();
    }

    public IEnumerable<Type> ListManagerTypes() {
        lock (_managerLock)
            return _managerLookup.Keys.ToArray();
    }

    public bool HasManager<T>() {
        lock (_managerLock)
            return _managerLookup.ContainsKey(typeof(T));
    }

    public T GetManager<T>() where T : ManagerBase {
        lock (_managerLock) {
            if (_managerLookup.TryGetValue(typeof(T), out var manager))
                return (T)manager;
            return null;
        }
    }

    internal bool TryGetManager(Type type, out ManagerBase manager) {
        lock (_managerLock)
            return _managerLookup.TryGetValue(type, out manager);
    }

    public T GetOrCreateManager<T>() where T : ManagerBase, new() {
        lock (_managerLock) {
            if (_managerLookup.TryGetValue(typeof(T), out var manager))
                return (T)manager;

            var type = typeof(T);
            var instance = new T();
            try {
                _managerLookup[type] = instance;
                instance.OnInstanceCreated();
                var handler = ManagerCreated;
                handler?.Invoke(this, instance);
                return instance;
            }
            catch (Exception ex) {
                _managerLookup.Remove(type);
                _logger.Error(ex, $"Error creating manager {type.Name}");
                return null;
            }
        }
    }

    public T GetModManager<T>() where T : ModManagerBase {
        lock (_managerLock) {
            if (_managerLookup.TryGetValue(typeof(T), out var manager))
                return (T)manager;
            return null;
        }
    }

    public ModManagerBase GetModManager() => _isCachedModManager ? GetModManager<ModManagerBase>() : null;

    internal void CacheModManager<T>(T manager) where T : ModManagerBase {
        lock (_managerLock) {
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
    }

    public void DestroyManager<T>() where T : ManagerBase {
        ManagerBase manager;
        lock (_managerLock) {
            if (!_managerLookup.TryGetValue(typeof(T), out manager)) return;
            _managerLookup.Remove(typeof(T));
        }
        var handler = ManagerDestroyed;
        handler?.Invoke(this, manager);
        manager.OnInstanceDestroy();
    }

    public void DestroyAllManagers() {
        ManagerBase[] managers;
        lock (_managerLock) {
            managers = _managerLookup.Values.ToArray();
            _managerLookup.Clear();
        }
        foreach (var manager in managers)
            try {
                var handler = ManagerDestroyed;
                handler?.Invoke(this, manager);
                manager.OnInstanceDestroy();
            }
            catch (Exception ex) {
                _logger.Error(ex, $"Error destroying {manager.GetType().Name}");
            }
    }

    public IEnumerable<T> FilterManagers<T>(Func<T, bool> predicate) where T : ManagerBase {
        lock (_managerLock)
            return _managerLookup.Values.OfType<T>().Where(predicate).ToArray();
    }

    public override string ToString() => Name;

    public void Dispose() {
        if (Disposed) return;
        Disposed = true;
        DestroyAllManagers();
        lock (AllDomainsLock) {
            AllDomains.Remove(Name);
            if (ReferenceEquals(_defaultDomain, this))
                _defaultDomain = null;
        }
    }
}
