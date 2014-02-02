using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using Hudl.Common.Extensions;
using Hudl.Config;
using Hudl.Mjolnir.Command;

namespace TestMvc45WebApp.Controllers
{
    // This is a 4.5-created webapp that's got <httpRuntime targetFramework="4.5"/>.

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
            public TestCommand() : base("test", "test", 5.Seconds()) { }
            protected override async Task<string> ExecuteAsync(CancellationToken cancellationToken)
            {
                await Task.Run(() => Thread.Sleep(1000));
                return "hello";
            }
        }
    }
}
