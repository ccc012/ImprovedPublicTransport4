using ColossalFramework.IO;
using CSLModsCommon.Utilities;
using System;
using System.IO;

namespace CSLModsCommon.Setting;

[AttributeUsage(AttributeTargets.Class)]
public class FileLocationAttribute : Attribute {
    public static string DefaultDirectory { get; } = BuildDefaultDirectory();
    public string Path { get; private set; }

    public FileLocationAttribute(string path) => Path = System.IO.Path.Combine(DefaultDirectory, path) + ".json";

    public FileLocationAttribute() => Path = System.IO.Path.Combine(DefaultDirectory, $"{AssemblyHelper.CurrentAssemblyName}Setting.json");

    private static string BuildDefaultDirectory() {
        var localApplicationData = TryGetGameLocalApplicationData();
        return System.IO.Path.Combine(System.IO.Path.Combine(localApplicationData, "ModsSettings"), AssemblyHelper.CurrentAssemblyName);
    }

    private static string TryGetGameLocalApplicationData() {
        try {
            if (!string.IsNullOrEmpty(DataLocation.localApplicationData)) {
                return DataLocation.localApplicationData;
            }
        }
        catch {
        }

        var fallbackRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(System.IO.Path.Combine(fallbackRoot, "Colossal Order"), "Cities_Skylines");
    }
}

