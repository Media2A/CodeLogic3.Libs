namespace CL.Core.Utilities
{
    /// <summary>
    /// Basic HTTP client helpers.
    /// </summary>
    public partial class CLU_Web
    {
        /// <summary>
        /// Performs a synchronous GET request and returns the response body.
        /// </summary>
        /// <param name="url">Target URL to request.</param>
        /// <returns>Response body as a string.</returns>
        public static string ClientRequest(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                return responseBody;
            }
        }

        /// <summary>
        /// Performs an asynchronous GET request and returns the response body.
        /// </summary>
        /// <param name="url">Target URL to request.</param>
        /// <returns>Response body as a string.</returns>
        public async Task<string> ClientRequestAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }
    }
}
