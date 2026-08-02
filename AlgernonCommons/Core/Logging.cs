// <copyright file="Logging.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace AlgernonCommons
{
    using System;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Logging utility class.
    /// </summary>
    public static class Logging
    {
        // Stringbuilder for messaging.
        private static readonly StringBuilder MessageBuilder = new StringBuilder(128);

        // Private fields.
        private static bool s_detailLogging = false;
        private static string s_modName;

        /// <summary>
        /// Exception event handler delegate.
        /// </summary>
        /// <param name="exceptionMessage">Exception message.</param>
        public delegate void ExceptionEventHandler(string exceptionMessage);

        /// <summary>
        /// Exception occured event.
        /// </summary>
        public static event ExceptionEventHandler EventExceptionOccured;

        /// <summary>
        /// Gets or sets a value indicating whether more detailed logging should be provided.
        /// If this is false, only errors, key messages, or exceptions will be logged (standard messages will be ignored).
        /// </summary>
        public static bool DetailLogging
        {
            get => s_detailLogging;

            set
            {
                s_detailLogging = value;
                KeyMessage("detailed logging ", value ? "enabled" : "disabled");
            }
        }

        /// <summary>
        /// Gets the mod's logging name (to identify each line).
        /// </summary>
        private static string ModName
        {
            get
            {
                // Check for cached reference.
                if (s_modName == null)
                {
                    // No cached reference - if ModBase defines a name, use that, otherwise fall back to the assembly name.
                    s_modName = ModBase.Instance?.LogName ?? AssemblyUtils.Name;
                }

                return s_modName;
            }
        }

        /// <summary>
        /// Prints a single-line debugging message to the Unity output log with an "ERROR: " prefix, regardless of the 'detailed logging' setting.
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        public static void Error(params object[] messages) => WriteMessage("ERROR: ", messages);

        /// <summary>
        /// Prints a single-line debugging message to the Unity output log, regardless of the 'detailed logging' setting.
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        public static void KeyMessage(params object[] messages) => WriteMessage(string.Empty, messages);

        /// <summary>
        /// Prints a single-line debugging message to the Unity output log if the 'detailed logging' option is set (otherwise does nothing).
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        public static void Message(params object[] messages)
        {
            if (s_detailLogging)
            {
                WriteMessage(string.Empty, messages);
            }
        }

        /// <summary>
        /// Prints an exception message to the Unity output log.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        public static void LogException(Exception exception, params object[] messages)
        {
            // Local builder: the shared MessageBuilder is not thread-safe and can interleave if
            // two log calls overlap (rare on the main thread, common if anything logs off-thread).
            var builder = new StringBuilder(256);
            builder.Append(ModName);
            builder.Append(": ");

            // Add each message parameter.
            if (messages != null)
            {
                for (int i = 0; i < messages.Length; ++i)
                {
                    // Append "null" in place of any null values.
                    builder.Append(messages[i] ?? "null");
                }
            }

            // Finish with a new line and the exception information.
            builder.AppendLine();
            builder.AppendLine("Exception: ");
            if (exception != null)
            {
                builder.AppendLine(exception.Message);
                builder.AppendLine(exception.Source);
                builder.AppendLine(exception.StackTrace);

                // Log inner exception as well, if there is one.
                if (exception.InnerException != null)
                {
                    builder.AppendLine("Inner exception:");
                    builder.AppendLine(exception.InnerException.Message);
                    builder.AppendLine(exception.InnerException.Source);
                    builder.AppendLine(exception.InnerException.StackTrace);
                }
            }
            else
            {
                builder.AppendLine("(null exception)");
            }

            // Write to log.
            Debug.Log(builder);

            // Invoke exception occured event.
            EventExceptionOccured?.Invoke(builder.ToString());
        }

        /// <summary>
        /// Prints a single-line debugging message to the Unity output log with a specified prefix.
        /// </summary>
        /// <param name="prefix">Prefix for message, if any.</param>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        private static void WriteMessage(string prefix, params object[] messages)
        {
            // Use StringBuilder for efficiency since we're doing a lot of manipulation here.
            // Start with mod name (to easily identify relevant messages), followed by colon to indicate start of actual message.
            MessageBuilder.Length = 0;
            MessageBuilder.Append(ModName);
            MessageBuilder.Append(": ");

            // Append prefix.
            MessageBuilder.Append(prefix);

            // Add each message parameter.
            for (int i = 0; i < messages.Length; ++i)
            {
                // Append "null" in place of any null values.
                MessageBuilder.Append(messages[i] ?? "null");
            }

            // Terminating period to confirm end of message.
            MessageBuilder.Append(".");

            Debug.Log(MessageBuilder);
        }
    }
}
