using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CSLModsCommon.Utilities;

public static class XmlHelper {
    public static void SerializeObjectToFile<T>(T obj, string filePath, XmlSerializerNamespaces xmlSerializerNamespaces) {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj), "Target object cannot be null or empty when serialize object to file");
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath), "Target file path cannot be null or empty when serialize object to file");
        using var writer = new StreamWriter(filePath);
        var serializer = new XmlSerializer(typeof(T));
        serializer.Serialize(writer, obj, xmlSerializerNamespaces);
    }

    public static void SerializeObjectToFile<T>(T obj, string filePath) {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj), "Target object cannot be null or empty when serialize object to file");
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath), "Target file path cannot be null or empty when serialize object to file");
        using var writer = new StreamWriter(filePath);
        var xmlSerializerNamespaces = new XmlSerializerNamespaces();
        xmlSerializerNamespaces.Add(string.Empty, string.Empty);
        var serializer = new XmlSerializer(typeof(T));
        serializer.Serialize(writer, obj, xmlSerializerNamespaces);
    }

    public static T DeserializeObjectFromFile<T>(string filePath) {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath), "Target file path cannot be null or empty when deserialize object from file");
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StreamReader(filePath);
        return (T)serializer.Deserialize(reader);
    }

    public static object DeserializeObjectFromFile(Type type, string path) {
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path), "Target file path cannot be null or empty when deserialize object from file");

        var serializer = new XmlSerializer(type);
        using var reader = new StreamReader(path);
        return serializer.Deserialize(reader);
    }

    public static void SerializeObjectToFile<T>(T obj, string filePath, bool format, string rootElementName = null, XmlSerializerNamespaces namespaces = null) {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj), "Target object cannot be null or empty when serialize object to file");
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath), "Target file path cannot be null or empty when serialize object to file");
        var xmlContent = SerializeObject(obj, format, rootElementName, namespaces);
        File.WriteAllText(filePath, xmlContent);
    }

    public static T DeserializeObjectFromFile<T>(string filePath, string rootElementName) {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(filePath, "Target file path cannot be null or empty when deserialize object from file");
        if (!File.Exists(filePath))
            throw new FileNotFoundException(filePath, "Target file path doesn't exit");
        var xmlContent = File.ReadAllText(filePath);
        return DeserializeObject<T>(xmlContent, rootElementName);
    }

    public static T DeserializeObject<T>(string xml, string rootElementName = null) {
        if (string.IsNullOrEmpty(xml))
            throw new ArgumentNullException(nameof(xml), "Xml string cannot be null or empty when deserialize object");
        var xmlSerializer = string.IsNullOrEmpty(rootElementName) ? new XmlSerializer(typeof(T)) : new XmlSerializer(typeof(T), new XmlRootAttribute(rootElementName));
        using var stringReader = new StringReader(xml);
        return (T)xmlSerializer.Deserialize(stringReader);
    }

    public static string SerializeObject<T>(T obj, bool format = false, string rootElementName = null, XmlSerializerNamespaces namespaces = null) {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj), "Target object cannot be null or empty when serialize object");
        var xmlSerializer = string.IsNullOrEmpty(rootElementName) ? new XmlSerializer(typeof(T)) : new XmlSerializer(typeof(T), new XmlRootAttribute(rootElementName));
        var settings = new XmlWriterSettings {
            Indent = format,
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false)
        };
        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        xmlSerializer.Serialize(xmlWriter, obj, namespaces);
        return stringWriter.ToString();
    }

    public static string ReadXmlFile(string filePath) {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(filePath, "Target file path cannot be null or empty when read the file");
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Target file path doesn't exit");
        return File.ReadAllText(filePath);
    }

    public static void WriteXmlToFile(string xml, string filePath) {
        if (string.IsNullOrEmpty(xml))
            throw new ArgumentNullException(xml, "Xml cannot be null or empty when write to file");
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(filePath, "Target file path cannot be null or empty when write to file");
        File.WriteAllText(filePath, xml);
    }
}