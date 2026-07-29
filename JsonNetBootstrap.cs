using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ImprovedPublicTransport
{
    internal static class JsonNetBootstrap
    {
        private const string NewtonsoftAssemblySimpleName = "Newtonsoft.Json";
        private const string RuntimeSerializationAssemblySimpleName = "System.Runtime.Serialization";
        private static bool _initialized;
        private static readonly object SyncRoot = new object();

        public static void EnsureLoaded()
        {
            if (_initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

                LoadBundledAssembly(NewtonsoftAssemblySimpleName);
                LoadBundledAssembly(RuntimeSerializationAssemblySimpleName);
                _initialized = true;
            }
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName requestedAssembly;
            try
            {
                requestedAssembly = new AssemblyName(args.Name);
            }
            catch
            {
                return null;
            }

            if (!string.Equals(requestedAssembly.Name, NewtonsoftAssemblySimpleName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(requestedAssembly.Name, RuntimeSerializationAssemblySimpleName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return LoadBundledAssembly(requestedAssembly.Name);
        }

        private static Assembly LoadBundledAssembly(string assemblySimpleName)
        {
            // Deliberately NOT "return whatever copy is already loaded". Other mods ship their own
            // Newtonsoft.Json (Real Time bundles a strong-named v13, while CSLModsCommon needs the
            // unsigned v9 we bundle) and whichever mod loaded first would otherwise win. Handing our
            // code a mismatched build makes JsonHelper throw a TypeInitializationException on
            // JsonWriter - which it silently swallows and returns an empty object for. That is what
            // broke settings persistence AND left every framework locale source empty. So: always
            // prefer the copy sitting next to our own DLL.
            string ourAssemblyPath = null;
            try
            {
                var ourDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                ourAssemblyPath = Path.Combine(ourDirectory ?? string.Empty, assemblySimpleName + ".dll");
            }
            catch
            {
                // Fall through to the already-loaded lookup below.
            }

            if (!string.IsNullOrEmpty(ourAssemblyPath) && File.Exists(ourAssemblyPath))
            {
                // Reuse our copy if it is already loaded, instead of loading the same file twice.
                var ours = FindLoadedAssembly(assemblySimpleName, ourAssemblyPath);
                if (ours != null)
                {
                    return ours;
                }

                try
                {
                    return Assembly.LoadFrom(ourAssemblyPath);
                }
                catch
                {
                    // Fall through - handing back someone else's copy beats handing back nothing.
                }
            }

            return FindLoadedAssembly(assemblySimpleName, null);
        }

        /// <summary>
        /// Finds a loaded assembly by simple name. When <paramref name="requiredPath"/> is provided,
        /// only matches the assembly actually loaded from that exact file.
        /// </summary>
        private static Assembly FindLoadedAssembly(string assemblySimpleName, string requiredPath)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(assembly =>
                {
                    try
                    {
                        if (!string.Equals(assembly.GetName().Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        if (string.IsNullOrEmpty(requiredPath))
                        {
                            return true;
                        }

                        // Dynamic assemblies throw on Location - treat those as "not ours".
                        return !string.IsNullOrEmpty(assembly.Location)
                            && string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(requiredPath), StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });
        }
    }
}
