using CSLModsCommon.Extension;
using System;

namespace CSLModsCommon;

public readonly struct FormattedString {
    public string Key { get; } = string.Empty;
    public object[] Args { get; }

    public FormattedString(string key, params object[] args) {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Args = args ?? ArrayExtensions.Empty<object>();
    }

    public override string ToString() => Key;

    public static implicit operator string(FormattedString fs) => fs.Key;

    public static implicit operator FormattedString(string key) => new(key);

    public string Format(Func<string, object[], string> parmFunc) => parmFunc?.Invoke(Key, Args) ?? Key;

    public FormattedString WithParams(params object[] extraArgs) {
        var combined = new object[Args.Length + extraArgs.Length];
        Args.CopyTo(combined, 0);
        extraArgs.CopyTo(combined, Args.Length);
        return new FormattedString(Key, combined);
    }
}