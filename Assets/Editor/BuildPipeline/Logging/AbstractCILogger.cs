// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Buildalon.Editor.BuildPipeline.Logging
{
    /// <summary>
    /// Base abstract logger to use when creating custom loggers.
    /// </summary>
    public abstract class AbstractCILogger : ICILogger
    {
#if UNITY_5_3_OR_NEWER
        private readonly ILogHandler defaultLogger;

        protected AbstractCILogger()
        {
            defaultLogger = DebugEx.unityLogger.logHandler;
            DebugEx.unityLogger.logHandler = this;
        }

        /// <inheritdoc />
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            var skip = false;

            foreach (var arg in args)
            {
                var message = arg as string;
                if (message != null)
                {
                    skip = StringEx.IsNullOrWhiteSpace(message) ||
                           CILoggingUtility.IgnoredLogs.Any(message.Contains);
                }
            }

            if (CILoggingUtility.LoggingEnabled && !skip)
            {
                switch (logType)
                {
                    case LogType.Log:
                        format = string.Format("\n{0}{1}{2}{3}", Log, LogColor, format, ResetColor);
                        break;
                    case LogType.Assert:
                    case LogType.Error:
                    case LogType.Exception:
                        format = string.Format("\n{0}{1}{2}{3}", Error, ErrorColor, format, ResetColor);
                        break;
                    case LogType.Warning:
                        format = string.Format("\n{0}{1}{2}{3}", Warning, WarningColor, format, ResetColor);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("logType", logType, null);
                }
            }

            defaultLogger.LogFormat(logType, context, format, args);
        }

        /// <inheritdoc />
        public void LogException(Exception exception, Object context)
        {
            if (CILoggingUtility.LoggingEnabled)
            {
                exception = new Exception(string.Format("\n{0}{1}", Error, exception.Message), exception);
            }

            defaultLogger.LogException(exception, context);
        }
#endif

        /// <inheritdoc />
        public virtual string Log
        {
            get { return string.Empty; }
        }

        /// <inheritdoc />
        public virtual string LogColor
        {
            get { return "\u001b[37m"; }
        }

        /// <inheritdoc />
        public virtual string Warning
        {
            get { return string.Empty; }
        }

        /// <inheritdoc />
        public virtual string WarningColor
        {
            get { return "\u001b[33m"; }
        }

        /// <inheritdoc />
        public virtual string Error
        {
            get { return string.Empty; }
        }

        /// <inheritdoc />
        public virtual string ErrorColor
        {
            get { return "\u001b[31m"; }
        }

        /// <inheritdoc />
        public virtual string ResetColor
        {
            get { return "\u001b[0m"; }
        }

        /// <inheritdoc />
        public virtual void GenerateBuildSummary(BuildReport buildReport, Stopwatch stopwatch)
        {
        }
    }
}
