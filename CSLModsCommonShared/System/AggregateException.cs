using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;

namespace System;

public class AggregateException : Exception {
    private const string DefaultMessage = "One or more errors occurred.";

    public ReadOnlyCollection<Exception> InnerExceptions { get; }

    public AggregateException()
        : base(DefaultMessage) => InnerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);

    public AggregateException(string message)
        : base(message) => InnerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);

    public AggregateException(string message, Exception innerException)
        : base(message, innerException) {
        if (innerException == null) throw new ArgumentNullException(nameof(innerException));

        InnerExceptions = new ReadOnlyCollection<Exception>(new[] { innerException });
    }

    public AggregateException(IEnumerable<Exception> innerExceptions) :
        this(DefaultMessage, innerExceptions) { }

    public AggregateException(params Exception[] innerExceptions) :
        this(DefaultMessage, innerExceptions) { }

    public AggregateException(string message, IEnumerable<Exception> innerExceptions)
        : this(message, innerExceptions as IList<Exception> ?? (innerExceptions == null ? null : new List<Exception>(innerExceptions))) { }

    public AggregateException(string message, params Exception[] innerExceptions) :
        this(message, (IList<Exception>)innerExceptions) { }

    private AggregateException(string message, IList<Exception> innerExceptions)
        : base(message, innerExceptions is { Count: > 0 } ? innerExceptions[0] : null) {
        if (innerExceptions == null) throw new ArgumentNullException(nameof(innerExceptions));

        var exceptionsCopy = new Exception[innerExceptions.Count];

        for (var i = 0; i < exceptionsCopy.Length; i++) {
            exceptionsCopy[i] = innerExceptions[i];

            if (exceptionsCopy[i] == null) throw new ArgumentException("An element of the inner exception array was null.");
        }

        InnerExceptions = new ReadOnlyCollection<Exception>(exceptionsCopy);
    }

    [SecurityCritical]
    protected AggregateException(SerializationInfo info, StreamingContext context) :
        base(info, context) {
        if (info == null) throw new ArgumentNullException(nameof(info));

        var innerExceptions = info.GetValue("InnerExceptions", typeof(Exception[])) as Exception[];
        if (innerExceptions == null) throw new SerializationException("AggregateException deserialization failed.");

        InnerExceptions = new ReadOnlyCollection<Exception>(innerExceptions);
    }

    [SecurityCritical]
    public override void GetObjectData(SerializationInfo info, StreamingContext context) {
        if (info == null) throw new ArgumentNullException(nameof(info));

        base.GetObjectData(info, context);

        var innerExceptions = new Exception[InnerExceptions.Count];
        InnerExceptions.CopyTo(innerExceptions, 0);
        info.AddValue("InnerExceptions", innerExceptions, typeof(Exception[]));
    }

    public override Exception GetBaseException() {
        Exception back = this;
        var backAsAggregate = this;
        while (backAsAggregate != null && backAsAggregate.InnerExceptions.Count == 1) {
            back = back.InnerException;
            backAsAggregate = back as AggregateException;
        }

        return back;
    }

    public void Handle(Func<Exception, bool> predicate) {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        List<Exception> unhandledExceptions = null;
        for (var i = 0; i < InnerExceptions.Count; i++) {
            if (predicate(InnerExceptions[i])) continue;
            unhandledExceptions ??= new List<Exception>();

            unhandledExceptions.Add(InnerExceptions[i]);
        }

        if (unhandledExceptions != null) throw new AggregateException(Message, unhandledExceptions);
    }

    public AggregateException Flatten() {
        var flattenedExceptions = new List<Exception>();

        var exceptionsToFlatten = new List<AggregateException> { this };
        var nDequeueIndex = 0;

        while (exceptionsToFlatten.Count > nDequeueIndex) {
            IList<Exception> currentInnerExceptions = exceptionsToFlatten[nDequeueIndex++].InnerExceptions;

            for (var i = 0; i < currentInnerExceptions.Count; i++) {
                var currentInnerException = currentInnerExceptions[i];

                if (currentInnerException == null) continue;

                if (currentInnerException is AggregateException currentInnerAsAggregate)
                    exceptionsToFlatten.Add(currentInnerAsAggregate);
                else
                    flattenedExceptions.Add(currentInnerException);
            }
        }

        return new AggregateException(Message, flattenedExceptions);
    }

    public override string ToString() {
        var text = base.ToString();
        const string aggregateExceptionFormat = "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}";

        for (var i = 0; i < InnerExceptions.Count; i++)
            text = string.Format(
                CultureInfo.InvariantCulture,
                aggregateExceptionFormat, text, Environment.NewLine, i, InnerExceptions[i].ToString(), "<---", Environment.NewLine);

        return text;
    }
}