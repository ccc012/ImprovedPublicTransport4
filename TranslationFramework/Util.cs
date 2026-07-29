using System;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Plugins;
using ICities;

namespace ImprovedPublicTransport.TranslationFramework
{
    public static class Util
    {
        /// <summary>
        /// Gets the on-disk folder this mod was loaded from.
        /// </summary>
        /// <param name="modType">
        /// The mod's <see cref="IUserMod"/> type. Only used as a hint - resolution falls back to
        /// locating the plugin by our own assembly, which keeps working even if the class that
        /// implements <see cref="IUserMod"/> changes (that exact mismatch previously broke both
        /// translation loading and the ticket-price icon atlases, because callers were still
        /// passing the pre-CSLModsCommon class).
        /// </param>
        public static string AssemblyPath(Type modType)
        {
            var pluginInfo = FindPluginByAssembly() ?? FindPluginByUserModType(modType);
            if (pluginInfo == null)
            {
                throw new Exception("Failed to locate this mod's plugin folder (assembly lookup and IUserMod type lookup both failed, type: " + modType + ")");
            }

            return pluginInfo.modPath;
        }

        /// <summary>
        /// Preferred lookup: ask the game which plugin owns the assembly this code lives in.
        /// Independent of which class happens to implement <see cref="IUserMod"/>.
        /// </summary>
        private static PluginManager.PluginInfo FindPluginByAssembly()
        {
            try
            {
                return Singleton<PluginManager>.instance.FindPluginInfo(Assembly.GetExecutingAssembly());
            }
            catch
            {
                return null;
            }
        }

        private static PluginManager.PluginInfo FindPluginByUserModType(Type modType)
        {
            if (modType == null)
            {
                return null;
            }

            try
            {
                foreach (var item in PluginManager.instance.GetPluginsInfo())
                {
                    try
                    {
                        var instances = item.GetInstances<IUserMod>();
                        if (modType == instances.FirstOrDefault()?.GetType())
                        {
                            return item;
                        }
                    }
                    catch
                    {
                        // A single misbehaving plugin shouldn't abort the search.
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}