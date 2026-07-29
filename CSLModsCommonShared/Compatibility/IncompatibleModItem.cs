using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using CSLModsCommon.Logging;
using CSLModsCommon.Utilities;
using System;
using System.IO;

namespace CSLModsCommon.Compatibility;

public class IncompatibleModItem {
    private readonly ILog _logger = LogManager.GetLogger();

    public string AssemblyName { get; set; }
    public IncompatibilityModLevel IncompatibilityLevel { get; set; }
    public PluginManager.PluginInfo Plugin { get; private set; }
    public PublishedFileId PublishedId => Plugin?.publishedFileID ?? PublishedFileId.invalid;
    public string ModPath => Plugin?.modPath;
    public bool IsPublished => PublishedId != PublishedFileId.invalid;
    public bool IsBuiltin => Plugin?.isBuiltin ?? false;
    public bool IsEnabled => Plugin?.isEnabled ?? false;
    public string DisplayName { get; set; }
    public bool IsDuplicateFunctionality { get; set; }
    public string AlternativeModName { get; set; }
    public string CustomWarningMessage { get; set; }

    public bool IsMatched {
        get {
            if (Plugin is null) return false;
            return IncompatibilityLevel == IncompatibilityModLevel.LoadNotAllowed || IsEnabled;
        }
    }

    public IncompatibleModItem(string assemblyName, IncompatibilityModLevel incompatibilityLevel, string displayName, bool isDuplicateFunctionality = true, string alternativeModName = null, string customWarningMessage = null) {
        AssemblyName = assemblyName;
        IncompatibilityLevel = incompatibilityLevel;
        DisplayName = displayName;
        IsDuplicateFunctionality = isDuplicateFunctionality;
        AlternativeModName = alternativeModName;
        CustomWarningMessage = customWarningMessage;
    }

    public override string ToString() => $"{nameof(IncompatibleModItem)}: Assembly name: '{AssemblyName}', path: '{ModPath}', isEnabled: '{IsEnabled}', incompatibility level: '{IncompatibilityLevel}', isBuiltin: '{IsBuiltin}', isPublished: '{IsPublished}', PublishedId: '{PublishedId}'";

    public void Assign(PluginManager.PluginInfo plugin) => Plugin = plugin;

    public void Unassign() => Plugin = null;

    public void Handle() {
        if (Plugin is null) return;

        try {
            _logger.Info($"Start handling incompatible mod: {AssemblyName}");
            Plugin.isEnabled = false;
            _logger.Info($"Disable incompatible mod: {AssemblyName}");
            if (IncompatibilityLevel == IncompatibilityModLevel.LoadNotAllowed && IsPublished) {
                PlatformService.workshop.Unsubscribe(PublishedId);
                _logger.Info($"Unsubscribe mod: {AssemblyName}");
            }

            if (IncompatibilityLevel == IncompatibilityModLevel.LoadNotAllowed && !IsPublished)
                try {
                    var directory = new DirectoryInfo(ModPath);
                    var folderName = directory.Name;
                    var targetFolderName = "_" + folderName;
                    if (directory.Parent != null) {
                        var targetDirectory = Path.Combine(directory.Parent.FullName, targetFolderName);
                        if (Directory.Exists(targetDirectory)) Directory.Delete(targetDirectory, true);

                        DirectoryHelper.CopyDirectory(ModPath, targetDirectory);
                        Directory.Delete(ModPath, true);
                        _logger.Info($"Exclude local mod {ModPath} to {targetDirectory}");
                    }
                }
                catch (IOException e) {
                    _logger.Error($"HandleMod, IO Exception: {e.Message}");
                }
        }
        catch (Exception e) {
            _logger.Error(e, "Handle mod failed");
        }
        finally {
            _logger.Info($"Finish handling incompatible mod: {AssemblyName}");
        }
    }
}