using CSLModsCommon.Serialization;
using CSLModsCommon.Setting;
using System.IO;

namespace CSLModsCommon.Manager;

public class SerializationManager : ManagerBase {
    public static readonly string SerializationPath = FileLocationAttribute.DefaultDirectory;
    private NewtonsoftReaderAdapter _newtonsoftReaderAdapter;
    private NewtonsoftWriterAdapter _newtonsoftWriterAdapter;

    private NewtonsoftReaderAdapter Reader => _newtonsoftReaderAdapter ??= new NewtonsoftReaderAdapter(SerializationPath);
    private NewtonsoftWriterAdapter Writer => _newtonsoftWriterAdapter ??= new NewtonsoftWriterAdapter(SerializationPath);

    public void SerializeData(ISerializable serializable) {
        EnsureSerializationDirectoryExists();
        serializable.Serialize(Writer);
    }

    public void DeserializeData(ISerializable serializable) {
        EnsureSerializationDirectoryExists();
        serializable.Deserialize(Reader);
    }

    private void EnsureSerializationDirectoryExists() {
        if (!Directory.Exists(SerializationPath)) Directory.CreateDirectory(SerializationPath);
    }
}