namespace CSLModsCommon.Compatibility; 
public readonly struct GameVersionCompatibility {
    public GameVersion MinGameVersion { get; }
    public GameVersion MaxGameVersion { get; }

    public GameVersionCompatibility(string gameVersion)
        : this(new GameVersion(gameVersion), new GameVersion(gameVersion)) { }

    public GameVersionCompatibility(GameVersion gameVersion)
        : this(gameVersion, gameVersion) { }

    public GameVersionCompatibility(string minVersion, string maxVersion)
        : this(new GameVersion(minVersion), new GameVersion(maxVersion)) { }

    public GameVersionCompatibility(int gameVersionMajor, int gameVersionMinor, int gameVersionPatch, int gameVersionBuild = 0)
        : this(new GameVersion(gameVersionMajor, gameVersionMinor, gameVersionPatch, gameVersionBuild)) { }

    public GameVersionCompatibility(GameVersion minGameVersion, GameVersion maxGameVersion) {
        MinGameVersion = minGameVersion;
        MaxGameVersion = maxGameVersion;
    }

    public override string ToString() => MinGameVersion.Equals(MaxGameVersion)
            ? $"{MinGameVersion}"
            : $"{MinGameVersion} - {MaxGameVersion}";

    public bool IsCompatible(GameVersion gameVersion) => gameVersion >= MinGameVersion && gameVersion <= MaxGameVersion;
}