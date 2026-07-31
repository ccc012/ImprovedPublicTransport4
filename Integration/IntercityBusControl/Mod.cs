using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;

namespace IntercityBusControl
{
    // Static config holder for IntercityBusControl integration.
    // Not an IUserMod so the IBC module won't appear as a separate mod in the Mods list.
    public static class Mod
    {
        public const string IntercityBusLine = "Intercity Bus Line";
        public const string Name = "Intercity Bus Control";
        public const string Description = "Intercity Bus Control";

        // Returns true when the Sunset Harbor DLC is owned.
        // Previously used reflection into ItemClassCollection.m_classDict looking for an
        // "Intercity Bus" key, which unreliably returned false even when the DLC was owned
        // (item class registration timing relative to this extension's OnLevelLoaded is not
        // guaranteed). SteamHelper.IsDLCOwned(UrbanDLC) is the same check already used
        // successfully elsewhere in this codebase (TicketPriceCustomizer) for the same DLC.
        public static bool IsSunsetHarborInstalled()
        {
            return SteamHelper.IsDLCOwned(SteamHelper.DLC.UrbanDLC);
        }

        private static readonly FieldInfo ClassDictField =
            typeof(ItemClassCollection).GetField("m_classDict", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Shared, cached reflection accessor for ItemClassCollection's private class dictionary -
        /// StationPatcher's retroactive sweep and InitializePrefabPatch's per-prefab postfix both need
        /// this same lookup; resolving the FieldInfo once here instead of independently in each avoids
        /// redoing the reflection call on every matching prefab during level/asset load.
        /// </summary>
        public static Dictionary<string, ItemClass> GetItemClassDict()
        {
            return ClassDictField?.GetValue(null) as Dictionary<string, ItemClass>;
        }
    }
}