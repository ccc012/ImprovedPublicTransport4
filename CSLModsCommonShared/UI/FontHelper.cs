using ColossalFramework.UI;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI;

public static class FontHelper {
    private static UIFont _regular;
    private static UIFont _semiBold;

    public static UIFont Regular => _regular ??= Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault(f => f.name == "OpenSans-Regular");

    public static UIFont SemiBold => _semiBold ??= Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault(f => f.name == "OpenSans-Semibold");
}