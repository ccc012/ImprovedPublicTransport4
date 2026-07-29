using CSLModsCommon.Manager;
using System.Collections.Generic;
using UnityEngine;

namespace CSLModsCommon.KeyBindings;

public class KeyBindingDispatcher : MonoBehaviour {
    private KeyBindingManager _manager;
    private List<KeyBindingEntry> _activeBindings = new();

    public bool Running { get; set; }

    private void Update() {
        if (!Running) return;
        if (!Input.anyKeyDown) return;
        for (var i = 0; i < _activeBindings.Count; i++) {
            var entry = _activeBindings[i];
            if (entry.Binding.IsPressed()) entry.Action?.Invoke();
        }
    }

    private void Awake() => _manager = Domain.DefaultDomain.GetOrCreateManager<KeyBindingManager>();

    private void OnEnable() => _manager.BindingsChanged += OnBindingsChanged;

    private void OnDisable() => _manager.BindingsChanged -= OnBindingsChanged;

    private void OnBindingsChanged(List<KeyBindingEntry> bindings) {
        Running = false;
        _activeBindings = bindings;
        Running = true;
    }
}