// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.Utils
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace ImprovedPublicTransport.Util
{
  public static class Utils
  {
    private static readonly string _fileName = "ImprovedPublicTransport4.log";
    private static readonly string _logPrefix = "ImprovedPublicTransport4: ";

    private static string LogFilePath
    {
      get
      {
        try
        {
          string logsDir = Path.Combine(Application.dataPath, "Logs");
          if (!Directory.Exists(logsDir))
          {
            Directory.CreateDirectory(logsDir);
          }

          return Path.Combine(logsDir, _fileName);
        }
        catch
        {
          return _fileName;
        }
      }
    }

      public static string AssemblyPath => PluginInfo.modPath;

      /// <summary>
      /// Locates this mod's plugin entry. Resolved from our own assembly rather than by looking for
      /// one specific IUserMod class: since the CSLModsCommon migration the IUserMod is
      /// <see cref="Mod"/>, not ImprovedPublicTransportMod, so hardcoding the latter threw during
      /// startup (AutoLineColor's GenericNames.Initialize is the first caller, from OnCreated).
      /// </summary>
      private static PluginManager.PluginInfo PluginInfo
      {
          get
          {
              try
              {
                  var byAssembly = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly());
                  if (byAssembly != null)
                  {
                      return byAssembly;
                  }
              }
              catch
              {
                  // Fall through to the IUserMod scan below.
              }

              foreach (var item in PluginManager.instance.GetPluginsInfo())
              {
                  try
                  {
                      var userMod = item.GetInstances<IUserMod>().FirstOrDefault();
                      if (userMod is Mod || userMod is ImprovedPublicTransportMod)
                      {
                          return item;
                      }
                  }
                  catch
                  {
                      // A single misbehaving plugin shouldn't abort the search.
                  }
              }

              throw new Exception("Failed to locate the ImprovedPublicTransport4 plugin folder.");
          }
      }

        /// <summary>
        /// Was a manual File.AppendAllText/WriteAllText pair targeting Logs/ImprovedPublicTransport4.log
        /// on every single Log/LogError/LogWarning call (hundreds of call sites across this mod).
        /// CSLModsCommon.Logging.LogManager's own Logger opens that exact same path with a
        /// StreamWriter it keeps open for the entire session (see Logger.cs) - every one of our
        /// calls was therefore guaranteed to hit a file-sharing violation, caught and silently
        /// turned into an extra "Error while writing to log file" Debug.LogWarning on top of the
        /// message we actually meant to log. That happened on effectively every Utils.Log* call in
        /// the mod, all session, every session - real, if modest, wasted CPU (a failed file open +
        /// exception + an extra Debug.LogWarning call) for zero benefit, since Debug.Log/LogWarning/
        /// LogError already land reliably in Unity's own output_log.txt regardless. Removed; this
        /// class no longer touches the filesystem for logging at all.
        /// </summary>
        public static void ClearLogFile()
    {
    }

    public static void LogToTxt(object o)
    {
      Utils.Log(o);
    }

    public static void Log(object o)
    {
      Utils.Log(PluginManager.MessageType.Message, o);
    }

    public static void LogError(object o)
    {
      Utils.Log(PluginManager.MessageType.Error, o);
    }

    public static void LogWarning(object o)
    {
      Utils.Log(PluginManager.MessageType.Warning, o);
    }

    private static void Log(PluginManager.MessageType type, object o)
    {
      string str = Utils._logPrefix + o;
      switch (type)
      {
        case PluginManager.MessageType.Error:
          Debug.LogError((object) str);
          break;
        case PluginManager.MessageType.Warning:
          Debug.LogWarning((object) str);
          break;
        case PluginManager.MessageType.Message:
          // Info spam via Debug.Log is the #1 FPS killer when Verbose is on - only mirror to the
          // Unity console when Verbose, matching the pre-existing behaviour for this level.
          if (Diagnostics.VerboseRuntimeLogs)
          {
            Debug.Log((object) str);
          }
          break;
      }
    }

    // Hot UI paths used to re-list every FieldInfo every frame (CityService panel, etc.).
    private static readonly Dictionary<string, FieldInfo> PrivateFieldCache =
      new Dictionary<string, FieldInfo>(32);

    public static Q GetPrivate<Q>(object o, string fieldName)
    {
      if (o == null || string.IsNullOrEmpty(fieldName))
      {
        return default(Q);
      }

      var type = o.GetType();
      var key = type.FullName + "\0" + fieldName;
      if (!PrivateFieldCache.TryGetValue(key, out var fieldInfo) || fieldInfo == null)
      {
        fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fieldInfo == null)
        {
          // Fallback: walk hierarchy once, then cache null to avoid repeating.
          for (var t = type; t != null && fieldInfo == null; t = t.BaseType)
          {
            fieldInfo = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
          }
        }

        PrivateFieldCache[key] = fieldInfo;
      }

      if (fieldInfo == null)
      {
        return default(Q);
      }

      return (Q) fieldInfo.GetValue(o);
    }

    public static float ToSingle(string value)
    {
      float result = 0.0f;
      float.TryParse(value, out result);
      return result;
    }

    public static int ToInt32(string value)
    {
      int result = 0;
      int.TryParse(value, out result);
      return result;
    }

    public static byte ToByte(string value)
    {
      byte result = 0;
      byte.TryParse(value, out result);
      return result;
    }

    // Default was a corrupted replacement character (U+FFFD / "♦") from an old decompile of an
    // ellipsis. Callers that omit suffix (StopListBoxRow) painted "Estação de ♦" for long PT names.
    public static bool Truncate(UILabel label, string text, string suffix = "…")
    {
      bool flag = false;
      try
      {
        using (UIFontRenderer renderer = label.ObtainRenderer())
        {
          float units = label.GetUIView().PixelsToUnits();
          float[] characterWidths = renderer.GetCharacterWidths(text);
          float num1 = 0.0f;
          float num2 = (float) ((double) label.width - (double) label.padding.horizontal - 2.0);
          for (int index = 0; index < characterWidths.Length; ++index)
          {
            num1 += characterWidths[index] / units;
            if ((double) num1 > (double) num2)
            {
              flag = true;
              int cut = index - 3;
              if (cut < 1)
                cut = Math.Max(1, index);
              text = text.Substring(0, cut) + suffix;
              break;
            }
          }
        }
        label.text = text;
      }
      catch
      {
        flag = false;
      }
      return flag;
    }

    public static string RemoveInvalidFileNameChars(string fileName)
    {
      return ((IEnumerable<char>) Path.GetInvalidFileNameChars()).Aggregate<char, string>(fileName, (Func<string, char, string>) ((current, c) => current.Replace(c.ToString(), string.Empty)));
    }

    public static int RoundToNearest(float value, int nearest)
    {
      return Mathf.RoundToInt(value / (float) nearest) * nearest;
    }

    public static bool AreParametersEqual(ParameterInfo[] sourceParameters, ParameterInfo[] destinationParameters)
    {
      if (sourceParameters.Length != destinationParameters.Length)
        return false;
      for (int index = 0; index < sourceParameters.Length; ++index)
      {
        if (!sourceParameters[index].ParameterType.IsAssignableFrom(destinationParameters[index].ParameterType))
          return false;
      }
      return true;
    }

    public static string GetModPath(string assemblyName, ulong workshopId)
    {
      foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
      {
        if (pluginInfo.name == assemblyName || (long) pluginInfo.publishedFileID.AsUInt64 == (long) workshopId)
          return pluginInfo.modPath;
      }
      return (string) null;
    }
    
    public static bool IsModActive(ulong modId)
    {
      try
      {
        var plugins = PluginManager.instance.GetPluginsInfo();
        return plugins.Any(p => p != null && p.isEnabled && p.publishedFileID.AsUInt64 == modId);
      }
      catch (Exception e)
      {
        UnityEngine.Debug.LogError($"Failed to detect if mod {modId} is active");
        UnityEngine.Debug.LogException(e);
        return false;
      }
    }
  }
}
