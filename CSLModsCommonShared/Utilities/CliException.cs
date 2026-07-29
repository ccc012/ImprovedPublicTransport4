using System;

namespace CSLModsCommon.Utilities;

public class CliException : Exception {
    public int ExitCode { get; }

    public CliException(int exitCode, string message) : base(message) => ExitCode = exitCode;

    public override string ToString() => $"CLI Error (Exit Code: {ExitCode}): {Message}";
}