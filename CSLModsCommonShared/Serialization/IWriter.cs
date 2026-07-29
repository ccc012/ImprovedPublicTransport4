namespace CSLModsCommon.Serialization;

public interface IWriter {
    void StartWrite(string fileName, string sectionName);
    void StartWrite(string fileName, string sectionName, WriteMode writeMode);
    void WriteProperty<T>(string name, T value);
    void RemoveSection(string sectionName);
    void RemoveOtherSections();
    void EndWrite();
}