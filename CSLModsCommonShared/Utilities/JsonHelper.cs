using CSLModsCommon.Logging;
using Newtonsoft.Json;
using System;
using System.IO;

namespace CSLModsCommon.Utilities; 
public static class JsonHelper {
    private static ILog Logger { get; } = LogManager.GetLogger();

    public static JsonSerializerSettings Settings { get; } = new() {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto
    };

    public static T DeserializeFromJsonFile<T>(string filePath) where T : new() {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath), $"File path cannot be null or empty when deserialize object from json file, type:{typeof(T).FullName}");
        if (!File.Exists(filePath)) {
            var typeName = typeof(T).FullName;
            Logger.Error($"File not found when deserialize object from json file, type: {typeName}, file path: {filePath}");
            Logger.Warn($"Create a new default object for return, type: {typeName}");
            return new T();
        }

        var json = File.ReadAllText(filePath);
        T t;
        try {
            t = JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch (Exception e) {
            var typeName = typeof(T).FullName;
            Logger.Error(e, $"Failed to deserialize object from json string: {typeName}");
            File.Delete(filePath);
            Logger.Info($"Delete file: {filePath}");
            t = new T();
            Logger.Warn($"Create a new default object for return: {typeName}");
        }

        return t;
    }

    public static T DeserializeFromJson<T>(string json) where T : new() {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentNullException(nameof(json), "Json string cannot be null or empty when deserialize from json");
        T result;
        try {
            result = JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch (Exception e) {
            var typeName = typeof(T).FullName;
            Logger.Error(e, $"Failed to deserialize object from json string: {typeName}");
            Logger.Warn($"Create a new default object: {typeName}");
            result = new T();
        }

        return result;
    }

    public static void SerializeToJsonFile(object obj, string filePath) {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty when serialize to json file");
        var directoryPath = Path.GetDirectoryName(filePath);
        if (directoryPath is null)
            throw new ArgumentNullException(nameof(directoryPath), "Directory path cannot be null when serialize to json file");
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        if (obj is null)
            throw new ArgumentNullException(nameof(obj), "Object cannot be null when serialize to json file");
        try {
            var json = JsonConvert.SerializeObject(obj, Settings);
            File.WriteAllText(filePath, json);
        }
        catch (Exception e) {
            Logger.Error(e, $"SerializeToJsonFile failed, path: {filePath}");
        }
    }

    public static string SerializeToJson(object obj) {
        try {
            if (obj != null) return JsonConvert.SerializeObject(obj, Settings);
            Logger.Error($"Object cannot be null when serialize to json");
            return string.Empty;
        }
        catch (Exception ex) {
            Logger.Error(ex, "Serialize failed");
            return string.Empty;
        }
    }
}