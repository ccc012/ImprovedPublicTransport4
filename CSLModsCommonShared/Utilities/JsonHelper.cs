using CSLModsCommon.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CSLModsCommon.Utilities; 
public static class JsonHelper {
    private static ILog Logger { get; } = LogManager.GetLogger();

    public static JsonSerializerSettings Settings { get; } = new() {
        Formatting = Newtonsoft.Json.Formatting.Indented,
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

        var content = File.ReadAllText(filePath);
        T t;
        try {
            t = DeserializeObject<T>(content);
        }
        catch (Exception e) {
            var typeName = typeof(T).FullName;
            Logger.Error(e, $"Failed to deserialize object from persisted content: {typeName}");
            Logger.Warn($"Keeping the original file untouched after deserialize failure: {filePath}");
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
            result = DeserializeObject<T>(json);
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
            var content = SerializeObject(obj);
            File.WriteAllText(filePath, content);
        }
        catch (Exception e) {
            Logger.Error(e, $"SerializeToJsonFile failed, path: {filePath}");
        }
    }

    public static string SerializeToJson(object obj) {
        try {
            if (obj != null) return SerializeObject(obj);
            Logger.Error("Object cannot be null when serialize to json");
            return string.Empty;
        }
        catch (Exception ex) {
            Logger.Error(ex, "Serialize failed");
            return string.Empty;
        }
    }

    private static T DeserializeObject<T>(string content) where T : new() {
        if (LooksLikeXml(content)) {
            return DeserializeXml<T>(content);
        }

        try {
            var result = JsonConvert.DeserializeObject<T>(content, Settings);
            return result is null ? new T() : result;
        }
        catch (Exception ex) {
            Logger.Warn(ex, $"Newtonsoft JSON deserialization failed for {typeof(T).FullName}, trying XML fallback.");
            return DeserializeXml<T>(content);
        }
    }

    private static string SerializeObject(object obj) {
        try {
            return JsonConvert.SerializeObject(obj, Settings);
        }
        catch (Exception ex) {
            Logger.Warn(ex, $"Newtonsoft JSON serialization failed for {obj.GetType().FullName}, trying XML fallback.");
            return SerializeXml(obj);
        }
    }

    private static T DeserializeXml<T>(string xml) where T : new() {
        using var stringReader = new StringReader(xml);
        var serializer = new XmlSerializer(typeof(T));
        var result = serializer.Deserialize(stringReader);
        return result is T typed ? typed : new T();
    }

    private static string SerializeXml(object obj) {
        using var stringWriter = new Utf8StringWriter();
        var settings = new XmlWriterSettings {
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8,
        };
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        var serializer = new XmlSerializer(obj.GetType());
        serializer.Serialize(xmlWriter, obj);
        return stringWriter.ToString();
    }

    private static bool LooksLikeXml(string content) {
        if (string.IsNullOrEmpty(content))
            return false;

        for (var i = 0; i < content.Length; i++) {
            if (char.IsWhiteSpace(content[i]))
                continue;

            return content[i] == '<';
        }

        return false;
    }

    private sealed class Utf8StringWriter : StringWriter {
        public override Encoding Encoding => Encoding.UTF8;
    }
}


