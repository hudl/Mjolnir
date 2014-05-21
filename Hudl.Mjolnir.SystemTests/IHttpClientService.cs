using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.SystemTests
{
    [Command.Attribute.Command(HttpClientService.TestKey, HttpClientService.TestKey, 10000)]
    internal interface IHttpClientService
    {
        Task<HttpStatusCode> MakeRequest(string url, CancellationToken token);
    }

    internal class HttpClientService : IHttpClientService
    {
        // Only use this (interface, class, key) for one test.
        // Re-using it will result in the same breaker/pool being used for multiple tests,
        // which isn't necessarily bad, but won't work right if different configurations
        // are needed or the breaker is still tripped from a previous test.
        public const string TestKey = "system-test-4";

        public async Task<HttpStatusCode> MakeRequest(string url, CancellationToken token)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url, token);
                var status = response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    return status;
                }

                throw new Exception("Status " + status);
            }
        }
    }
}
