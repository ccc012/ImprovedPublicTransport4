namespace CSLModsCommon.Compatibility;

public interface IVersionModRule {
    void Set(GameVersionCompatibility item);
    void Set(int gameVersionMajor, int gameVersionMinor, int gameVersionPatch, int gameVersionBuild = 0);
}