using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.SystemTests
{
    internal class HttpClientCommand : Command<HttpStatusCode>
    {
        private readonly string _url;

        public HttpClientCommand(string key, string url, TimeSpan timeout) : base(key, key, timeout)
        {
            _url = url;
        }

        protected override async Task<HttpStatusCode> ExecuteAsync(CancellationToken cancellationToken)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(_url, cancellationToken);
            client.Dispose();
            var status = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return status;
            }

            throw new Exception("Status " + status);
        }
    }
}