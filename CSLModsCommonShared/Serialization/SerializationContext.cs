using Newtonsoft.Json.Linq;

namespace CSLModsCommon.Serialization; 
public class SerializationContext {
    public string FilePath { get; }
    public string SectionName { get; }
    public JObject Root { get; }
    public JObject Current { get; }

    public SerializationContext(string filePath, string sectionName, JObject root, JObject current) {
        FilePath = filePath;
        SectionName = sectionName;
        Root = root;
        Current = current;
    }
}