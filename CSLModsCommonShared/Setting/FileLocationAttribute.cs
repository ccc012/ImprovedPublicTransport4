using ColossalFramework.IO;
using CSLModsCommon.Utilities;
using System;
using SystemIO = System.IO;

namespace CSLModsCommon.Setting;

[AttributeUsage(AttributeTargets.Class)]
public class FileLocationAttribute : Attribute {
    public static string DefaultDirectory { get; } = SystemIO.Path.Combine(SystemIO.Path.Combine(DataLocation.localApplicationData, "ModsSettings"), AssemblyHelper.CurrentAssemblyName);
    public string Path { get; private set; }

    public FileLocationAttribute(string path) => Path = SystemIO.Path.Combine(DefaultDirectory, path) + ".json";

    public FileLocationAttribute() => Path = SystemIO.Path.Combine(DefaultDirectory, $"{AssemblyHelper.CurrentAssemblyName}Setting.json");
}