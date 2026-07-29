using System;
using System.IO;
using System.Reflection;
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
        static Mod()
        {
            JsonNetBootstrap.EnsureLoaded();

            // Cities: Skylines loads every enabled mod's DLLs into the SAME process/AppDomain -
            // there's no per-mod isolation. Several other mods bundle their own Newtonsoft.Json,
            // sometimes a different build than the v9 copy CSLModsCommon expects here. Force our
            // bundled copy to register as early as possible before any JSON-backed settings/UI work.
            try
            {
                var ourDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var ourNewtonsoftPath = Path.Combine(ourDirectory ?? string.Empty, "Newtonsoft.Json.dll");
                if (File.Exists(ourNewtonsoftPath))
                {
                    Assembly.LoadFrom(ourNewtonsoftPath);
                }
            }
            catch
            {
                // Best-effort only - JsonNetBootstrap's AssemblyResolve hook remains active.
            }
        }
    }
}
