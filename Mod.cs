using CSLModsCommon;

namespace ImprovedPublicTransport
{
    // Entry point for the CSLModsCommon options UI (version badge, changelog,
    // compatibility warnings, translation-progress language switcher). The
    // actual gameplay patches/integrations are unrelated and untouched - they
    // still live on ImprovedPublicTransportMod (ICities.IUserMod + LoadingExtensionBase),
    // which the game discovers independently of this class.
    public class Mod : ModEntry<IptModManager>
    {
    }
}
