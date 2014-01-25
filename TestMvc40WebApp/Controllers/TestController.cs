using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Key;

namespace TestMvc40WebApp.Controllers
{
    // This is a 4.0-created webapp that's retargeted at 4.5, except hasn't been given <httpRuntime targetFramework="4.5"/>
    // It has, however, been given <add key="aspnet:UseTaskFriendlySynchronizationContext" value="true"/>, which should improve
    // task behavior.

    public class TestController : Controller
    {
        public async Task<ActionResult> Context()
        {
            ConfigurationUtility.Init();

            // This should not throw a NullReferenceException if the following is in Web.config:
            //   <add key="aspnet:UseTaskFriendlySynchronizationContext" value="true"/>

            var path = System.Web.HttpContext.Current.Request.PathInfo;
            var result = await new TestCommand().InvokeAsync();

            // Without the above property, HttpContext.Current will be null.
            path = System.Web.HttpContext.Current.Request.PathInfo;
            return Content("Success: " + path);
        }

        private class TestCommand : Command<string>
        {
            public TestCommand() : base(GroupKey.Named("test"), TimeSpan.FromSeconds(5)) {}
            protected override async Task<string> ExecuteAsync(CancellationToken cancellationToken)
            {
                await Task.Run(() => Thread.Sleep(1000));
                return "hello";
            }
        }
    }
}
