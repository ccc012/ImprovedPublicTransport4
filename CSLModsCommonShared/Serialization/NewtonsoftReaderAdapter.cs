using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace CSLModsCommon.Serialization;

public class NewtonsoftReaderAdapter : IReader {
    private readonly string _baseDir;
    private SerializationContext _context;

    public NewtonsoftReaderAdapter(string baseDir) {
        if (string.IsNullOrEmpty(baseDir))
            throw new ArgumentNullException(nameof(baseDir));
        _baseDir = baseDir;
    }

    public void StartRead(string fileName, string sectionName) {
        var filePath = Path.Combine(_baseDir, $"{fileName}.json");

        JObject root;
        JObject current;

        if (!File.Exists(filePath)) {
            root = new JObject();
            current = new JObject();
        }
        else {
            root = JObject.Parse(File.ReadAllText(filePath));
            if (root[sectionName] is JObject section)
                current = section;
            else
                current = new JObject();
        }

        _context = new SerializationContext(filePath, sectionName, root, current);
    }

    public void GetProperty<T>(string name, ref T value) {
        if (_context == null)
            throw new InvalidOperationException("Call StartRead first");

        if (!_context.Current.TryGetValue(name, out var token)) return;

        var obj = token.ToObject<T>();
        if (obj != null) value = obj;
    }

    public T GetProperty<T>(string name) {
        if (_context == null)
            throw new InvalidOperationException("Call StartRead first");

        if (!_context.Current.TryGetValue(name, out var token)) return default!;
        try {
            return token.ToObject<T>();
        }
        catch {
            return default!;
        }
    }

    public void EndRead() => _context = null;
}