using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CL.Core.Utilities
{
    /// <summary>
    /// Web session helper utilities.
    /// </summary>
    public partial class CLU_Web
    {
        // Session Strings
        /// <summary>
        /// Gets a string value from the current session.
        /// </summary>
        /// <param name="SessionKey">Session key to retrieve.</param>
        /// <returns>Stored value or null if missing.</returns>
        public static string GetSession(string SessionKey)
        {
            var SessionContent = CLU_Web.HC().Session.GetString(SessionKey);
            return SessionContent;
        }

        /// <summary>
        /// Stores a string value in the current session.
        /// </summary>
        /// <param name="SessionKey">Session key to set.</param>
        /// <param name="SessionValue">Value to store.</param>
        public static void SetSession(string SessionKey, string SessionValue)
        {
            CLU_Web.HC().Session.SetString(SessionKey, SessionValue);
        }

        // Session Objects
        /// <summary>
        /// Stores a serialized object in the current session.
        /// </summary>
        /// <param name="key">Session key to set.</param>
        /// <param name="value">Object value to serialize and store.</param>
        public static void SetSessionObject<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            CLU_Web.HC().Session.SetString(key, json);
        }

        /// <summary>
        /// Gets a deserialized object from the current session.
        /// </summary>
        /// <param name="key">Session key to retrieve.</param>
        /// <returns>Deserialized value or default if missing.</returns>
        public static T GetSessionObject<T>(string key)
        {
            var value = CLU_Web.HC().Session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// Removes a session entry by key.
        /// </summary>
        /// <param name="SessionKey">Session key to remove.</param>
        public static void RemoveSession(string SessionKey)
        {
            CLU_Web.HC().Session.Remove(SessionKey);
        }

        /// <summary>
        /// Clears all session entries.
        /// </summary>
        public static void ClearSession()
        {
            CLU_Web.HC().Session.Clear();
        }

        /// <summary>
        /// Gets the current session identifier.
        /// </summary>
        /// <returns>Session ID string.</returns>
        public static string GetSessionID()
        {
            return CLU_Web.HC().Session.Id.ToString();
        }
    }
}
