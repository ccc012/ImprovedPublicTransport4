using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace CSLModsCommon.Utilities; 
public sealed class CliExecutor : IDisposable {
    private readonly Process _process;
    private readonly StringBuilder _output = new();
    private readonly StringBuilder _error = new();
    private bool _captureOutput = true;
    private bool _throwOnError;
    private int _timeoutMilliseconds = Timeout.Infinite;
    private bool _isBackgroundExecution;
    private readonly Stopwatch _stopwatch;
    private Action<string> _outputCallback;
    private Action<string> _errorCallback;
    private Action<CliResult> _exitCallback;

    public CliExecutor(string executable) {
        _process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        _stopwatch = new Stopwatch();
    }

    public static CliExecutor Create(string executable) => new CliExecutor(executable);

    public CliExecutor WithArgument(string argument) {
        if (argument == null) throw new ArgumentNullException(nameof(argument));
        _process.StartInfo.Arguments += " " + argument;
        return this;
    }

    public CliExecutor WithArguments(params string[] arguments) {
        foreach (var arg in arguments) WithArgument(arg);

        return this;
    }

    public CliExecutor WithWorkingDirectory(string workingDirectory) {
        if (!Directory.Exists(workingDirectory))
            throw new DirectoryNotFoundException("Working directory not found: " + workingDirectory);

        _process.StartInfo.WorkingDirectory = workingDirectory;
        return this;
    }

    public CliExecutor WithEnvironmentVariable(string name, string value) {
        _process.StartInfo.EnvironmentVariables[name] = value;
        return this;
    }

    public CliExecutor ThrowOnError(bool throwOnError = true) {
        _throwOnError = throwOnError;
        return this;
    }

    public CliExecutor CaptureOutput(bool capture = true) {
        _captureOutput = capture;
        return this;
    }

    public CliExecutor OnOutput(Action<string> callback) {
        _outputCallback = callback;
        return this;
    }

    public CliExecutor OnError(Action<string> callback) {
        _errorCallback = callback;
        return this;
    }

    public CliExecutor OnExit(Action<CliResult> callback) {
        _exitCallback = callback;
        return this;
    }

    public CliResult Execute() {
        _stopwatch.Reset();
        _stopwatch.Start();
        _process.StartInfo.Arguments = _process.StartInfo.Arguments.Trim();
        _process.EnableRaisingEvents = false;
        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnErrorDataReceived;

        try {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            if (!_process.WaitForExit(_timeoutMilliseconds)) {
                KillProcess();
                throw new TimeoutException($"Command timed out after {_timeoutMilliseconds}ms");
            }

            _process.WaitForExit();

            if (_throwOnError && _process.ExitCode != 0) throw new CliException(_process.ExitCode, _error.ToString());

            var result = new CliResult(
                _process.ExitCode,
                _output.ToString(),
                _error.ToString(),
                _process.StartInfo.FileName,
                _process.StartInfo.Arguments,
                _stopwatch.Elapsed);
            _exitCallback?.Invoke(result);
            return result;
        }
        catch (Win32Exception ex) {
            return new CliResult(
                ex.NativeErrorCode,
                _output.ToString(),
                ex.Message,
                _process.StartInfo.FileName,
                _process.StartInfo.Arguments,
                _stopwatch.Elapsed);
        }
        catch (Exception ex) {
            return new CliResult(
                -1,
                _output.ToString(),
                ex.Message,
                _process.StartInfo.FileName,
                _process.StartInfo.Arguments,
                _stopwatch.Elapsed);
        }
        finally {
            _stopwatch.Stop();
            _process.OutputDataReceived -= OnOutputDataReceived;
            _process.ErrorDataReceived -= OnErrorDataReceived;
            DisposeProcess();
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs args) {
        if (args.Data == null) return;
        if (_captureOutput) _output.AppendLine(args.Data);
        _outputCallback?.Invoke(args.Data);
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs args) {
        if (args.Data == null) return;
        if (_captureOutput) _error.AppendLine(args.Data);
        _errorCallback?.Invoke(args.Data);
    }

    public CliExecutor WithTimeout(int milliseconds) {
        _timeoutMilliseconds = milliseconds;
        return this;
    }

    public void ExecuteInBackground() {
        _isBackgroundExecution = true;
        ThreadPool.QueueUserWorkItem(_ => {
            try {
                ExecuteInternal();
            }
            catch (Exception ex) {
                _errorCallback?.Invoke("Background execution failed: " + ex.Message);
            }
        });
    }

    private void ExecuteInternal() {
        _stopwatch.Reset();
        _stopwatch.Start();
        _process.StartInfo.Arguments = _process.StartInfo.Arguments.Trim();
        _process.EnableRaisingEvents = true;

        var outputWaitHandle = new AutoResetEvent(false);
        var errorWaitHandle = new AutoResetEvent(false);

        _process.OutputDataReceived += (_, args) => {
            if (args.Data == null) {
                outputWaitHandle.Set();
            }
            else {
                if (_captureOutput) _output.AppendLine(args.Data);
                _outputCallback?.Invoke(args.Data);
            }
        };

        _process.ErrorDataReceived += (_, args) => {
            if (args.Data == null) {
                errorWaitHandle.Set();
            }
            else {
                if (_captureOutput) _error.AppendLine(args.Data);
                _errorCallback?.Invoke(args.Data);
            }
        };

        _process.Exited += (_, _) => {
            _stopwatch.Stop();
            outputWaitHandle.WaitOne(2000);
            errorWaitHandle.WaitOne(2000);
            _exitCallback?.Invoke(new CliResult(
                _process.ExitCode,
                _output.ToString(),
                _error.ToString(),
                _process.StartInfo.FileName,
                _process.StartInfo.Arguments,
                _stopwatch.Elapsed));

            DisposeProcess();
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private void KillProcess() {
        try {
            if (!_process.HasExited) {
                _process.Kill();
                _process.WaitForExit(1000);
            }
        }
        catch (InvalidOperationException) { }
        catch (Exception ex) {
            if (_errorCallback != null)
                _errorCallback("Failed to kill process: " + ex.Message);
        }
    }

    public void CleanupBackgroundProcess() {
        if (_isBackgroundExecution) DisposeProcess();
    }

    private void DisposeProcess() {
        try {
            if (!_process.HasExited) {
                _process.Kill();
                _process.WaitForExit(1000);
            }
        }
        catch {
            // ignore
        }

        try {
            _process.Dispose();
        }
        catch {
            // ignore
        }
    }

    public void Dispose() => DisposeProcess();
}