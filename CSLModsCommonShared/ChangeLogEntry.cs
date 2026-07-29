using System;

namespace CSLModsCommon; 
public readonly struct ChangelogEntry : IEquatable<ChangelogEntry> {
    public ChangelogFlag Flag { get; }
    public FormattedString Description { get; }
    public bool LocalizedDescription { get; }

    public ChangelogEntry(ChangelogFlag flag, string description, bool localizedDescription = true) {
        Flag = flag;
        Description = description;
        LocalizedDescription = localizedDescription;
    }

    public ChangelogEntry(ChangelogFlag flag, FormattedString formattedLocalized) {
        Flag = flag;
        Description = formattedLocalized;
        LocalizedDescription = true;
    }

    public override string ToString() => $"{Flag}: {Description}";

    public bool Equals(ChangelogEntry other) => Flag == other.Flag && Description == other.Description;
}