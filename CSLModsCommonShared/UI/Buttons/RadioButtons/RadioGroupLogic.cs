using System;
using System.Collections.Generic;
using System.Linq;

namespace CSLModsCommon.UI.Buttons.RadioButtons; 
public class RadioGroupLogic<T> {
    private readonly List<RadioButtonItem<T>> _items = new();
    public RadioButtonItem<T> SelectedButton { get; private set; }
    public T SelectedItem => SelectedButton.Value;

    public event Action<RadioButtonItem<T>> SelectionChanged;

    public static RadioGroupLogic<T> Create() => new RadioGroupLogic<T>();

    public RadioGroupLogic<T> AddRange(params RadioButtonItem<T>[] items) {
        foreach (var item in items)
            Add(item);
        return this;
    }

    public RadioGroupLogic<T> Add(RadioButtonItem<T> item) {
        if (_items.Contains(item)) return this;
        _items.Add(item);
        item.CheckedChanged += OnButtonChecked;
        return this;
    }

    public RadioGroupLogic<T> SetDefault(Func<RadioButtonItem<T>, bool> predicate) {
        var button = _items.FirstOrDefault(predicate);
        if (button == null) return this;
        button.IsChecked = true;
        SelectedButton = button;
        return this;
    }

    public RadioGroupLogic<T> SetDefault(T item) => SetDefault(b => EqualityComparer<T>.Default.Equals(b.Value, item));

    private void OnButtonChecked(RadioButtonItem<T> button) {
        if (!button.IsChecked) return;

        foreach (var other in _items.Where(other => other != button))
            other.IsChecked = false;

        SelectedButton = button;
        SelectionChanged?.Invoke(button);
    }
}