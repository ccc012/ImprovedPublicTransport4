using CSLModsCommon.Logging;
using CSLModsCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSLModsCommon.Manager;

public abstract class ComponentManagerBase {
    protected static readonly ILog Logger = LogManager.GetLogger(AssemblyHelper.CurrentAssemblyName);
    private GameObject _rootObject;
    private CoroutineRunner _runner;
    private Coroutine _updateLoop;
    private readonly HashSet<Coroutine> _activeCoroutines = new();

    public Domain Domain => Domain.DefaultDomain;
    public Type ManagerType { get; private set; }

    protected GameObject RootObject {
        get {
            if (_rootObject != null) return _rootObject;
            var name = $"{GetType().FullName}_Root";
            _rootObject = GameObject.Find(name) ?? new GameObject(name);
            Object.DontDestroyOnLoad(_rootObject);
            return _rootObject;
        }
    }

    protected CoroutineRunner Runner {
        get {
            if (_runner != null) return _runner;
            _runner = RootObject.GetComponent<CoroutineRunner>();
            if (_runner == null)
                _runner = RootObject.AddComponent<CoroutineRunner>();
            return _runner;
        }
    }

    protected virtual void OnCreate() => Logger.Info($"Manager created: {ManagerType.FullName}");

    protected virtual void OnDestroy() => Logger.Info($"Manager destroyed: {ManagerType.FullName}");

    protected virtual void OnStartRunning() { }
    protected virtual void OnUpdate() { }
    protected virtual float UpdateInterval => 0f;
    protected virtual void OnStopRunning() { }

    public bool IsRunning(Coroutine coroutine) => _activeCoroutines.Contains(coroutine);

    public void EnableUpdateLoop() {
        if (_updateLoop != null) return;
        OnStartRunning();
        _updateLoop ??= StartRoutine(UpdateLoop());
    }

    public void DisableUpdateLoop() {
        if (_updateLoop == null) return;
        StopRoutine(_updateLoop);
        _updateLoop = null;
        OnStopRunning();
    }

    public void StopAllRoutines() {
        foreach (var coroutine in _activeCoroutines)
            Runner.StopCoroutine(coroutine);
        _activeCoroutines.Clear();
    }

    internal virtual void OnInstanceCreated() {
        ManagerType = GetType();
        OnCreate();
    }

    internal virtual void OnInstanceDestroy() => OnDestroy();

    protected bool TryStartRoutine(IEnumerator routine, out Coroutine coroutine) {
        coroutine = null;
        if (!Runner.enabled) return false;
        coroutine = Runner.StartCoroutine(routine);
        _activeCoroutines.Add(coroutine);
        return true;
    }

    protected Coroutine StartRoutine(IEnumerator routine) {
        if (!Runner.enabled) {
            Logger.Warn($"{ManagerType.FullName}.CoroutineRunner is disabled. Cannot start routine: {routine}");
            return null;
        }

        var c = Runner.StartCoroutine(routine);
        _activeCoroutines.Add(c);
        return c;
    }

    protected void StopRoutine(Coroutine routine) {
        if (_runner == null || routine == null) return;
        Runner.StopCoroutine(routine);
        _activeCoroutines.Remove(routine);
    }

    protected void SetCoroutineEnabled(bool enabled) {
        if (_runner != null)
            _runner.enabled = enabled;
    }

    protected void InvokeNextFrame(Action action) => StartRoutine(InvokeNextFrameImpl(action));

    protected void DelayRoutine(float seconds, Action action) => StartRoutine(DelayImpl(seconds, action));

    protected Coroutine RepeatRoutine(float interval, Func<bool> stopCondition, Action action) => StartRoutine(RepeatImpl(interval, stopCondition, action));

    private IEnumerator InvokeNextFrameImpl(Action action) {
        yield return null;
        action?.Invoke();
    }

    private IEnumerator DelayImpl(float seconds, Action action) {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }

    private IEnumerator RepeatImpl(float interval, Func<bool> stopCondition, Action action) {
        while (stopCondition == null || !stopCondition()) {
            yield return new WaitForSeconds(interval);
            action?.Invoke();
        }
    }

    private IEnumerator UpdateLoop() {
        while (true) {
            yield return UpdateInterval > 0f ? new WaitForSeconds(UpdateInterval) : null;
            OnUpdate();
        }
    }
}