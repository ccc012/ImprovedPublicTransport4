using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons;

public class ToggleButton : IconButton {
    public event UIElementEventHandler<ToggleButton, bool> ToggleChanged;

    protected bool _isOn;
    protected ToggleVisualSet _offVisuals;
    protected ToggleVisualSet _onVisuals;

    public ToggleVisualSet OffVisuals => _offVisuals;
    public ToggleVisualSet OnVisuals => _onVisuals;

    public bool IsOn {
        get => _isOn;
        set {
            if (_isOn != value) OnToggleChanged(value);
        }
    }

    protected virtual void OnToggleChanged(bool value) {
        if (!isEnabled) return;
        _isOn = value;
        ToggleChanged?.Invoke(this, value);
        Invalidate();
    }

    public override void Awake() {
        base.Awake();
        _offVisuals = new ToggleVisualSet(this);
        _onVisuals = new ToggleVisualSet(this);
    }

    protected override void OnRebuildRenderData() {
        RenderBackground();
        RenderForeground();
    }

    protected override Color32 GetFgRenderColor() => IsOn
            ? _onVisuals.FgColors.GetValue(State)
            : _offVisuals.FgColors.GetValue(State);

    protected override Color32 GetBgRenderColor() => IsOn
            ? _onVisuals.BgColors.GetValue(State)
            : _offVisuals.BgColors.GetValue(State);

    protected override UITextureAtlas.SpriteInfo GetFgSprite() {
        if (_fgAtlas is null) return null;
        return IsOn
            ? _fgAtlas[_onVisuals.FgSprites.GetValue(State)]
            : _fgAtlas[_offVisuals.FgSprites.GetValue(State)];
    }

    protected override UITextureAtlas.SpriteInfo GetBgSprite() {
        if (_bgAtlas is null) return null;
        return IsOn
            ? _bgAtlas[_onVisuals.BgSprites.GetValue(State)]
            : _bgAtlas[_offVisuals.BgSprites.GetValue(State)];
    }
}