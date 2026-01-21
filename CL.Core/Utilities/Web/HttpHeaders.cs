using System;
using System.Net;

namespace CL.Core.Utilities
{
    /// <summary>
    /// HTTP header helper utilities.
    /// </summary>
    public partial class CLU_Web
    {

        /// <summary>
        /// Sets a response header and verifies it matches the request header.
        /// </summary>
        /// <param name="key">Header name.</param>
        /// <param name="value">Header value to set.</param>
        /// <returns>True when the request header matches the value after setting.</returns>
        public static bool SetHttpHeader(string key, string value)
        {
            HC().Response.Headers[key] = value;

            if (HC().Request.Headers[key] == value)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a request header value by name.
        /// </summary>
        /// <param name="key">Header name.</param>
        /// <param name="value">Unused parameter (kept for API compatibility).</param>
        /// <returns>Header value as a string.</returns>
        public static string GetHttpHeader(string key, string value)
        {
            var header = HC().Request.Headers[key];

            return header;
        }
    }
}
