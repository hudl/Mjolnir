using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.SystemTests
{
    [Command.Attribute.Command("system-test-4", "system-test-4", 10000)]
    internal interface IHttpClientService
    {
        Task<HttpStatusCode> MakeRequest(string url, CancellationToken token);
    }

    internal class HttpClientService : IHttpClientService
    {
        public async Task<HttpStatusCode> MakeRequest(string url, CancellationToken token)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(url, token);
            var status = response.StatusCode;
            client.Dispose();
            if (response.IsSuccessStatusCode)
            {
                return status;
            }

            throw new Exception("Status " + status);
        }
    }
}
