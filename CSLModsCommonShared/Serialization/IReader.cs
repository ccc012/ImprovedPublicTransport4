namespace CSLModsCommon.Serialization;

public interface IReader {
    void StartRead(string fileName, string sectionName);
    void GetProperty<T>(string name, ref T value);
    T GetProperty<T>(string name);
    void EndRead();
}