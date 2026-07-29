using System;
using System.Diagnostics;

namespace CSLModsCommon.Extension;

public class CapturedStackException : Exception {
    public override string StackTrace { get; }

    public CapturedStackException(string message) : base(message) => StackTrace = new StackTrace(true).ToString();

    public CapturedStackException() => StackTrace = new StackTrace(true).ToString();
}