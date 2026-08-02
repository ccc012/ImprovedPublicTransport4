using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ImprovedPublicTransport.Util;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal abstract class ColorSetLoaderBase : IColorSetLoader
    {
        [NotNull]
        public string Name { get; }

        [NotNull] private readonly string _filename;
        [NotNull] private readonly string _defaultContent;
        [CanBeNull] private IColorSet _cachedColorSet;

        protected ColorSetLoaderBase([NotNull] string name, [NotNull] string filename, [NotNull] string defaultContent)
        {
            Name = name;
            _filename = filename;
            _defaultContent = defaultContent;
        }

        public IColorSet LoadColorSet()
        {
            // Each KnownColorSet loader is a per-file singleton (see KnownColorSet.cs) reused for
            // the lifetime of the process, and the color file is never modified externally while the
            // game runs - re-reading and re-parsing it from disk on every single call (once per line
            // needing a colour, from both the periodic ColorMonitor.OnUpdate pass and the manual
            // "reassign colour" button) was pure repeated I/O for output that never changes.
            if (_cachedColorSet != null)
            {
                return _cachedColorSet;
            }

            var logger = Console.Instance;

            var modConfigPath = Path.Combine(Path.Combine(Utils.AssemblyPath, "Resources"), "AutoLineColor");
            var fullPath = Path.Combine(modConfigPath, _filename);
            logger.Message($"Loading color set from {fullPath}");
            var unparsedColors = _defaultContent;

            try
            {
                if (File.Exists(fullPath))
                {
                    unparsedColors = File.ReadAllText(fullPath);
                }
                else
                {
                    logger.Message("No colors found, writing default values to  " + fullPath);
                    Directory.CreateDirectory(modConfigPath);
                    File.WriteAllText(fullPath, unparsedColors);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error reading colors from disk: " + ex);
            }

            try
            {
                _cachedColorSet = ParseColorSet(unparsedColors);
                return _cachedColorSet;
            }
            catch (Exception ex)
            {
                logger.Error("Error parsing loaded colors: " + ex);
                logger.Message("Color file content: " + unparsedColors);
                throw;
            }
        }

        [NotNull] protected abstract IColorSet ParseColorSet([NotNull] string unparsedColors);

        protected static bool TryHexToColor(string hex, out Color32 color)
        {
            try
            {
                hex = hex.Replace("0x", "");
                hex = hex.Replace("#", "");
                hex = hex.Trim();

                byte alpha = 255;

                var red = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                var green = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                var blue = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

                if (hex.Length == 8)
                {
                    alpha = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                }

                color = new Color32(red, green, blue, alpha);
                return true;
            }
            catch (Exception)
            {
                color = new Color32(0, 0, 0, 255);
                return false;
            }
        }
    }
}
