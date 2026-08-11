from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
ui_path = root / "CSLModsCommonShared" / "UI" / "UITextBase.cs"
guard_path = root / "HarmonyPatches" / "EngineHardening" / "FontRequestCharactersGuardPatch.cs"
train_path = root / "Integration" / "TrainDisplayUpdated" / "TrainDisplayIntegration.cs"
options_path = root / "CSLModsCommonShared" / "Manager" / "OptionsPanelManager.cs"


def failures(ui, guard, train, options):
    callback = re.search(r"private void OnFontTextureRebuilt\(Font font\) \{([\s\S]*?)\n    \}", ui)
    callback_body = callback.group(1) if callback else ""
    checks = {
        "font callback filters the rebuilt font": "uiDynamicFont.baseFont != font" in callback_body,
        "font callback only marks deferred work": "_fontTextureRebuildPending = true" in callback_body,
        "font callback does not request glyphs synchronously": "RequestCharacterInfo" not in callback_body,
        "next Update consumes deferred font work": re.search(r"override void Update\(\)[\s\S]*?_fontTextureRebuildPending = false;[\s\S]*?RequestCharacterInfo\(\)", ui),
        "font callback unbind ignores current font state": re.search(r"UnbindTextureRebuildCallback\(\)[\s\S]*?if \(!_isFontCallbackAssigned\) return;[\s\S]*?textureRebuilt -= OnFontTextureRebuilt", ui),
        "guard uses finalizer cleanup": "nameof(Finalizer)" in guard and re.search(r"Finalizer\(.*bool __state", guard),
        "guard no longer relies on postfix cleanup": "nameof(Postfix)" not in guard,
        "detailed train markers avoid decorative glyphs": not re.search(r'"[●→↘↓]"', train),
        "locale options refresh is deferred": re.search(r"OnLocaleChanged\(\)\s*=>\s*_localeRefreshPending = true", options),
        "options Update consumes coalesced refresh": re.search(r"Update\(\)[\s\S]*?_localeRefreshPending = false;[\s\S]*?Refresh\(\)", options),
    }
    return [name for name, passed in checks.items() if not passed]


sources = [path.read_text(encoding="utf-8-sig") for path in (ui_path, guard_path, train_path, options_path)]
failed = failures(*sources)
if failed:
    raise SystemExit("Font stack overflow hardening verification failed: " + "; ".join(failed))

mutations = (
    (sources[0].replace("_fontTextureRebuildPending = true", "RequestCharacterInfo()", 1), *sources[1:]),
    (sources[0], sources[1].replace("nameof(Finalizer)", "nameof(Postfix)", 1), *sources[2:]),
    (*sources[:2], sources[2].replace('DrawDetailedStopMarker(', 'GUI.Label(new Rect(contentLeft, bodyY, iconW, rowH), "●", _tinyStyle); DrawDetailedStopMarker(', 1), sources[3]),
    (*sources[:3], sources[3].replace("_localeRefreshPending = true", "Refresh()", 1)),
)
if any(not failures(*mutation) for mutation in mutations):
    raise SystemExit("Font stack overflow hardening harness is not red-capable")

print("Font stack overflow hardening harness passed; legacy-pattern mutations fail.")
