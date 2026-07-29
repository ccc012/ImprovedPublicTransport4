using ColossalFramework.UI;
using CSLModsCommon.Setting;
using CSLModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSLModsCommon.Manager;

public sealed class SettingManager : ManagerBase {
    private Dictionary<Type, SettingSource> _settings;

    protected override void OnCreate() {
        base.OnCreate();
        _settings = new Dictionary<Type, SettingSource>();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        _settings.Clear();
        _settings = null;
    }

    public T GetSetting<T>() where T : ISetting, new() {
        if (_settings.TryGetValue(typeof(T), out var setting))
            return (T)setting.Setting;
        return new T();
    }

    public ModSettingBase GetDefaultSetting() {
        foreach (var kv in _settings)
            if (kv.Value.IsDefault)
                return (ModSettingBase)kv.Value.Setting;

        return null;
    }

    public T GetOrCreateSetting<T>() where T : ISetting, new() {
        if (_settings.TryGetValue(typeof(T), out var source)) return (T)source.Setting;

        var path = GetFileLocation<T>();
        if (string.IsNullOrEmpty(path)) {
            Logger.Error("Loading settings failed because no file location was provided.");
            return default;
        }

        var setting = new T();
        _settings.Add(typeof(T), new SettingSource(path, setting));
        return setting;
    }

    public bool ResetSettingDefaults(Type type) {
        if (!_settings.TryGetValue(type, out var source))
            return false;
        source.Setting.SetDefaults();
        return true;
    }

    public bool ResetSettingDefaults<T>() where T : ISetting, new() {
        if (!_settings.TryGetValue(typeof(T), out var source))
            return false;
        source.Setting.SetDefaults();
        return true;
    }

    public void SetSettingsDefaults() => _settings.ForEach(v => v.Value.Setting.SetDefaults());

    public T Load<T>(bool overrider = false) where T : ISetting, new() {
        var path = GetFileLocation<T>();
        if (string.IsNullOrEmpty(path)) {
            Logger.Error("Loading settings failed because no file location was provided.");
            return default;
        }

        T setting;
        if (File.Exists(path)) {
            var instance = JsonHelper.DeserializeFromJsonFile<T>(path);
            if (instance is not null) {
                setting = instance;
                Logger.Info($"Local setting detected, deserialized, path: {path}");
            }
            else {
                setting = new T();
                Logger.Info("Unable to load the setting file, generate default setting");
            }
        }
        else {
            setting = new T();
            Logger.Info("No settings file found, generate default setting");
        }

        var type = typeof(T);
        if (_settings.ContainsKey(type)) {
            if (overrider)
                _settings.Remove(type);
            else
                return (T)_settings[type].Setting;
        }

        if (type.IsSubclassOf(typeof(ModSettingBase)) && !type.IsAbstract)
            _settings[type] = new SettingSource(true, path, setting);
        else
            _settings[type] = new SettingSource(path, setting);

        Logger.Info($"Loaded [{type.FullName}] setting");
        return setting;
    }

    public void SaveDefaultSetting() => _settings.Where(v => v.Value.IsDefault).ForEach(v => Save(v.Value.Setting));

    public void SaveSettings() => _settings.ForEach(v => Save(v.Value.Setting));

    public void Save<T>(T t) where T : ISetting {
        if (t is null) {
            Logger.Error("Unable to save null setting");
            return;
        }

        var type = t.GetType();

        if (_settings.TryGetValue(type, out var settingSource)) {
            JsonHelper.SerializeToJsonFile(t, settingSource.Path);
            Logger.Verbose($"Saved setting, path: {settingSource.Path}");
            return;
        }

        var path = GetFileLocation(type);
        if (string.IsNullOrEmpty(path)) {
            Logger.Error("Path is empty or null when trying to save setting");
            return;
        }

        if (string.IsNullOrEmpty(Path.GetDirectoryName(path))) {
            Logger.Error("Directory is empty or null when trying to save setting");
            return;
        }

        try {
            JsonHelper.SerializeToJsonFile(t, path);
            Logger.Verbose($"Saved setting, path: {path}");
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Unable to serialize setting to file, path: {path}");
        }
    }

    public static string GetFileLocation<T>() => ((FileLocationAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(FileLocationAttribute)))?.Path;

    public static string GetFileLocation(Type type) => ((FileLocationAttribute)Attribute.GetCustomAttribute(type, typeof(FileLocationAttribute)))?.Path;
}