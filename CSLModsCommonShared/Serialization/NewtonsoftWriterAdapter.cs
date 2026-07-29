using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace CSLModsCommon.Serialization; 
public class NewtonsoftWriterAdapter : IWriter {
    private readonly string _baseDir;
    private SerializationContext _context;
    private WriteMode _writeMode = WriteMode.Overwrite;

    public NewtonsoftWriterAdapter(string baseDir) {
        if (string.IsNullOrEmpty(baseDir))
            throw new ArgumentNullException(nameof(baseDir));
        _baseDir = baseDir;
    }

    public void StartWrite(string fileName, string sectionName, WriteMode writeMode) {
        StartWrite(fileName, sectionName);
        _writeMode = writeMode;
    }

    public void StartWrite(string fileName, string sectionName) {
        Directory.CreateDirectory(_baseDir);
        var filePath = Path.Combine(_baseDir, $"{fileName}.json");
        var root = File.Exists(filePath) ? JObject.Parse(File.ReadAllText(filePath)) : new JObject();
        _context = new SerializationContext(filePath, sectionName, root, new JObject());
    }

    public void WriteProperty<T>(string name, T value) {
        if (_context == null)
            throw new InvalidOperationException("Call StartWrite first");

        _context.Current[name] = value != null ? JToken.FromObject(value) : JValue.CreateNull();
    }

    public void RemoveSection(string sectionName) {
        if (_context == null)
            throw new InvalidOperationException("Call StartWrite first");

        _context.Root.Remove(sectionName);
    }

    public void RemoveOtherSections() {
        if (_context == null)
            throw new InvalidOperationException("Call StartWrite first");

        var keysToRemove = _context.Root.Properties()
            .Where(p => p.Name != _context.SectionName)
            .Select(p => p.Name)
            .ToList();

        foreach (var key in keysToRemove)
            _context.Root.Remove(key);
    }

    public void EndWrite() {
        if (_context.Root == null || _context.Current == null || _context.SectionName == null)
            throw new InvalidOperationException("StartWrite not called");

        switch (_writeMode) {
            case WriteMode.Overwrite:
                _context.Root[_context.SectionName] = _context.Current;
                break;

            case WriteMode.Merge:
                if (_context.Root[_context.SectionName] is JObject existingSection) {
                    foreach (var prop in _context.Current.Properties())
                        existingSection[prop.Name] = prop.Value;
                    _context.Root[_context.SectionName] = existingSection;
                }
                else {
                    _context.Root[_context.SectionName] = _context.Current;
                }

                break;

            case WriteMode.Append:
                if (_context.Root[_context.SectionName] is JObject existingSection2) {
                    foreach (var prop in _context.Current.Properties())
                        if (!existingSection2.TryGetValue(prop.Name, out _))
                            existingSection2[prop.Name] = prop.Value;
                    _context.Root[_context.SectionName] = existingSection2;
                }
                else {
                    _context.Root[_context.SectionName] = _context.Current;
                }

                break;
            default:
                _context.Root[_context.SectionName] = _context.Current;
                break;
        }

        File.WriteAllText(_context.FilePath, _context.Root.ToString(Formatting.Indented));

        _context = null;
    }
}