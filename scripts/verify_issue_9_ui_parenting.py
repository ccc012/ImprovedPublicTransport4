from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
mod = (root / "ImprovedPublicTransportMod.cs").read_text(encoding="utf-8-sig")
editor = (root / "UI" / "VehicleEditor.cs").read_text(encoding="utf-8-sig")
manager = (root / "UI" / "PrefabPanelManager.cs").read_text(encoding="utf-8-sig")


def failures(mod_source, editor_source, manager_source):
    checks = {
        "VehicleEditor uses IptGameObject lifecycle": "IptGameObject.AddComponent<VehicleEditor>();" in mod_source,
        "VehicleEditor is not parented through UIView": not re.search(
            r"(?:objectOfType|UIView\.GetAView\(\))\.AddUIComponent\(typeof\(VehicleEditor\)\)", mod_source
        ),
        "No separate VehicleEditor lifecycle field": "_vehicleEditor" not in mod_source,
        "Prefab panel resolves editor from IptGameObject":
            "ImprovedPublicTransportMod.IptGameObject?.GetComponent<VehicleEditor>()" in manager_source,
        "Dropdown retains original parent and selection event": all(token in editor_source for token in (
            "DropDown.Create((UIComponent) uiPanel5)",
            "dropDown.DropDownPanelAlignParent = (UIComponent) this;",
            "dropDown.eventSelectedItemChanged += changedEventHandler1;",
        )),
        "Apply and Default buttons are root children":
            editor_source.count("UIUtils.CreateButton((UIComponent) this)") == 2,
        "Buttons are not ButtonRow children":
            "UIUtils.CreateButton((UIComponent) uiPanel11)" not in editor_source,
        "Buttons use normal eventClick handlers": all(token in editor_source for token in (
            "button1.eventClick += mouseEventHandler1;",
            "button2.eventClick += mouseEventHandler2;",
            "button1.isInteractive = true;",
            "button1.enabled = true;",
            "button2.isInteractive = true;",
            "button2.enabled = true;",
        )),
        "Root button positions are deterministic": all(token in editor_source for token in (
            "private void UpdateButtonPositions()",
            "const float rightSideLeft = 60f;",
            "const float captionHeight = 30f;",
            "const float assetRowHeight = 34f;",
            "const float fieldRowsHeight = 5f * 30f;",
            "this._applyButton.relativePosition = new Vector3(x, y);",
            "this._defaultButton.relativePosition = new Vector3(x + buttonWidth + buttonSpacing, y);",
        )),
        "Only buttons are brought forward": not re.search(r"(?:uiPanel|_rightSidePanel|_buttonRow)\w*\.BringToFront\(\)", editor_source),
        "Layout refresh and coroutine workarounds removed": all(token not in editor_source for token in (
            "RefreshLayout", "ScheduleLayoutRefresh", "PerformLayout", "Coroutine", "yield return null",
        )),
        "Panel interaction suppression removed": "isInteractive = false" not in editor_source,
        "Manual click fallback and diagnostics removed": all(token not in editor_source for token in (
            "OnVehicleEditorMouseDown", "OnRightSidePanelMouseDown", "ContainsPoint(", "UseButtonEvent(",
            "_handledButton", "_handlingRootFallback", "eventParam.Use()", "[DEBUG-VE-",
        )),
        "VehicleEditor Awake workaround removed": "public override void Awake()" not in editor_source,
        "Apply normalizes capacity before reading values": re.search(
            r"OnCapacitySubmitted\(uiTextField1, uiTextField1\.text, prefab\);\s*"
            r"int capacity = Utils\.ToInt32\(uiTextField1\.text\)", editor_source
        ),
    }
    return [name for name, passed in checks.items() if not passed]


failed = failures(mod, editor, manager)
if failed:
    raise SystemExit("Issue #9 UI parenting verification failed: " + "; ".join(failed))

mutations = (
    (mod.replace("IptGameObject.AddComponent<VehicleEditor>();", "objectOfType.AddUIComponent(typeof(VehicleEditor));", 1), editor, manager),
    (mod, editor.replace("UIUtils.CreateButton((UIComponent) this)", "UIUtils.CreateButton((UIComponent) uiPanel11)", 1), manager),
    (mod, editor.replace("button1.eventClick += mouseEventHandler1;", "", 1), manager),
    (mod, editor + "\nprivate void RefreshLayout() { this.PerformLayout(); }\n", manager),
    (mod, editor.replace("this.OnCapacitySubmitted(uiTextField1, uiTextField1.text, prefab);", "", 1), manager),
)
if any(not failures(*mutation) for mutation in mutations):
    raise SystemExit("Issue #9 UI parenting harness is not red-capable")

print("Issue #9 UI parenting harness passed; structural mutations fail.")
