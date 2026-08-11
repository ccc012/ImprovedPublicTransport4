from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
panel = (root / "UI" / "CSLModsCommonOptionsPanel.cs").read_text(encoding="utf-8-sig")
section = (root / "CSLModsCommonShared" / "UI" / "OptionsPanel" / "OptionsPanelBase.SettingsSection.cs").read_text(encoding="utf-8-sig")
actions = (root / "Settings" / "SettingsActions.cs").read_text(encoding="utf-8-sig")
errors = []

for callback_site in (
    r"checkBox\.CheckChanged \+= \(c, v\) => InvokeSettingChanged\(\(\) => callback\?\.Invoke\(c, v\)\);",
    r"slider\.ValueChanged \+= \(_, v\) => InvokeSettingChanged\(\(\) => callback\?\.Invoke\(v\)\);",
    r"card\.Control\.EventValueChanged \+= \(_, v\) => InvokeSettingChanged\(\(\) => callback\?\.Invoke\(v\)\);",
    r"logic\.LogicSelectionChanged \+= \(_, v\) => InvokeSettingChanged\(\(\) => onChanged\?\.Invoke\(v\)\);",
):
    if not re.search(callback_site, section):
        errors.append("missing centralized callback wrapper: " + callback_site)

if "SettingChanged?.Invoke();" not in section:
    errors.append("central wrapper does not notify panel")
if "section.SettingChanged = SettingsActions.MarkCustomAndSave;" not in panel:
    errors.append("IPT panel sections are not wired to manual persistence")
if "panel.SettingChanged = OnSettingChanged;" in (root / "CSLModsCommonShared" / "UI" / "OptionsPanel" / "OptionsPanelBase.cs").read_text(encoding="utf-8-sig"):
    errors.append("framework-owned sections must not change gameplay profile")
if "section.SettingChanged = SettingsActions.MarkCustomAndSave;" not in panel:
    errors.append("manual changes do not mark Custom and save")
if "profileSection.NotifySettingChanged = false;" not in panel:
    errors.append("gameplay profile selector is not excluded from manual-change hook")
if not re.search(r"if \(settings\.GameplayProfile == profile && profile != ModSetting\.GameplayProfiles\.Custom\)", actions):
    errors.append("reselecting Custom cannot flush settings")
if not re.search(r"public static void MarkCustomAndSave\(\).*?GameplayProfile = ModSetting\.GameplayProfiles\.Custom;.*?SaveSettings\(\);", actions, re.S):
    errors.append("MarkCustomAndSave does not set profile and persist")

if errors:
    for error in errors:
        print("FAIL: " + error)
    sys.exit(1)
print("gameplay profile persistence verifier: PASS")
