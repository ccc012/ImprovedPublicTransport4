using System;

namespace CSLModsCommon.Utilities; 
public class CliResult {
    public int ExitCode { get; }
    public string StandardOutput { get; }
    public string StandardError { get; }
    public string Command { get; }
    public string Arguments { get; }
    public TimeSpan ExecutionTime { get; }

    public bool Success => ExitCode == 0;

    public CliResult(int exitCode, string standardOutput, string standardError, string command, string arguments, TimeSpan executionTime) {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
        Command = command;
        Arguments = arguments;
        ExecutionTime = executionTime;
    }

    public void EnsureSuccess() {
        if (!Success) throw new CliException(ExitCode, StandardError);
    }
}