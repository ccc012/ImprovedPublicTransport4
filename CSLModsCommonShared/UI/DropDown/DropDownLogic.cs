using System;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.UI.DropDown;

public class DropDownLogic<T> : IDropDownLogic {
    public event Action<int, DropDownItem<T>> LogicSelectionChanged;
    public event Action<int, string> SelectionChanged;
    public event Action ItemEnabledChanged;

    private readonly List<DropDownItem<T>> _items = new();
    private List<string> _visibleLabelsCache = new();

    public IList<DropDownItem<T>> Items => _items;
    public IList<string> VisibleLabels => _visibleLabelsCache;
    public int SelectedIndex { get; private set; } = -1;
    public DropDownItem<T> SelectedItem => SelectedIndex >= 0 && SelectedIndex < _items.Count ? _items[SelectedIndex] : null;

    private DropDownLogic() { }

    public static DropDownLogic<T> Create() => new DropDownLogic<T>();

    public void SelectIndex(int index) => Select(index);

    public DropDownLogic<T> SetItems(IEnumerable<DropDownItem<T>> items, int defaultIndex = 0) {
        foreach (var item in _items) item.EnabledChanged -= OnItemEnabledChangedInternal;

        _items.Clear();
        _items.AddRange(items);

        foreach (var item in _items) item.EnabledChanged += OnItemEnabledChangedInternal;

        RebuildVisibleLabels();

        if (_items.Count > 0) {
            if (defaultIndex < 0 || defaultIndex >= _items.Count || !_items[defaultIndex].IsEnabled) defaultIndex = _items.FindIndex(x => x.IsEnabled);

            Select(defaultIndex, false);
        }

        return this;
    }

    public DropDownLogic<T> SetItems(IEnumerable<DropDownItem<T>> items, Func<DropDownItem<T>, bool> defaultSelector) {
        foreach (var item in _items) item.EnabledChanged -= OnItemEnabledChangedInternal;

        _items.Clear();
        _items.AddRange(items);

        foreach (var item in _items) item.EnabledChanged += OnItemEnabledChangedInternal;

        RebuildVisibleLabels();

        if (_items.Count > 0) {
            var defaultIndex = _items.FindIndex(x => defaultSelector?.Invoke(x) ?? false);
            if (defaultIndex < 0 || !_items[defaultIndex].IsEnabled) defaultIndex = _items.FindIndex(x => x.IsEnabled);

            Select(defaultIndex, false);
        }

        return this;
    }

    public DropDownLogic<T> Select(int index, bool notify = true) {
        if (index < 0 || index >= _items.Count || !_items[index].IsEnabled || index == SelectedIndex) return this;
        SelectedIndex = index;
        if (!notify) return this;
        LogicSelectionChanged?.Invoke(index, _items[index]);
        SelectionChanged?.Invoke(index, _items[index].DisplayText);
        return this;
    }

    public IEnumerable<DropDownItem<T>> GetVisibleItems() => _items.Where(x => x.IsEnabled);

    public void SetItemEnabled(int index, bool enabled) {
        if (index < 0 || index >= _items.Count) return;
        _items[index].IsEnabled = enabled;
    }

    private void OnItemEnabledChangedInternal(DropDownItem<T> _) {
        RebuildVisibleLabels();
        ItemEnabledChanged?.Invoke();
    }

    private void RebuildVisibleLabels() => _visibleLabelsCache = _items.Where(x => x.IsEnabled).Select(x => x.DisplayText).ToList();
}