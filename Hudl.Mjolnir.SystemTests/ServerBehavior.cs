using System;
using System.Net;
using System.Threading;

namespace Hudl.Mjolnir.SystemTests
{
    internal static class ServerBehavior
    {
        public static Action<HttpListenerContext> Immediate200()
        {
            return context =>
            {
                context.Response.StatusCode = (int) HttpStatusCode.OK;
                context.Response.Close();
            };
        }

        public static Action<HttpListenerContext> Immediate500()
        {
            return context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            };
        }

        public static Action<HttpListenerContext> Delayed200(TimeSpan sleep)
        {
            return context =>
            {
                Thread.Sleep(sleep);
                context.Response.StatusCode = (int) HttpStatusCode.OK;
                context.Response.Close();
            };
        }

        public static Action<HttpListenerContext> Percentage500(int percent)
        {
            return context =>
            {
                var success = (new Random().Next(0, 100)) < percent;
                context.Response.StatusCode = (int) (success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
                context.Response.Close();
            };
        }
    }
}