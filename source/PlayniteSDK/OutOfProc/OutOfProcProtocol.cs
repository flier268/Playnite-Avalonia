using System;

namespace Playnite.SDK.OutOfProc
{
    /// <summary>
    /// Minimal, AOT-friendly out-of-proc add-on protocol constants (newline-delimited JSON over stdio).
    /// </summary>
    public static class OutOfProcProtocol
    {
        /// <summary>
        /// Current protocol version.
        /// </summary>
        public const int ProtocolVersion = 1;

        /// <summary>
        /// Request JSON property name for request id (string).
        /// </summary>
        public const string RequestIdProperty = "id";

        /// <summary>
        /// Request JSON property name for method (string).
        /// </summary>
        public const string RequestMethodProperty = "method";

        /// <summary>
        /// Request JSON property name for params (any JSON value).
        /// </summary>
        public const string RequestParamsProperty = "params";

        /// <summary>
        /// Response JSON property name for result (any JSON value).
        /// </summary>
        public const string ResponseResultProperty = "result";

        /// <summary>
        /// Response JSON property name for error object.
        /// </summary>
        public const string ResponseErrorProperty = "error";

        /// <summary>
        /// Error object property name for numeric error code.
        /// </summary>
        public const string ErrorCodeProperty = "code";

        /// <summary>
        /// Error object property name for human readable message.
        /// </summary>
        public const string ErrorMessageProperty = "message";

        /// <summary>
        /// Creates a new request id.
        /// </summary>
        public static string NewRequestId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static class Methods
        {
            public const string Ping = "ping";
            public const string GenericGetCommands = "generic.getCommands";
            public const string GenericRunCommand = "generic.runCommand";
        }
    }
}
