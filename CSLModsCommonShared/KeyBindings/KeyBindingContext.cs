using System;

namespace CSLModsCommon.KeyBindings;

[Flags]
public enum KeyBindingContext {
    None = 0,
    MainMenu = 1 << 0,
    Game = 1 << 1,
    MapEditor = 1 << 2,
    AssetEditor = 1 << 3,
    ThemeEditor = 1 << 4,
    ScenarioEditor = 1 << 5,
    InGame = Game | MapEditor | AssetEditor | ThemeEditor | ScenarioEditor,
    Global = MainMenu | InGame
}