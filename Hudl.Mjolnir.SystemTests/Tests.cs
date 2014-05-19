using System.IO;
using System.Linq;
using System.Net;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Command.Attribute;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hudl.Mjolnir.SystemTests
{
    public class Tests
    {
        private const ushort ServerPort = 22222;
        private const string ReportFile = @"c:\hudl\logs\mjolnir-system-test-report.html";

        private static readonly ILog Log = LogManager.GetLogger(typeof (Tests));

        private readonly IConfigurationProvider _testConfigProvider = new SystemTestConfigProvider();
        private readonly MemoryStoreRiemann _testRiemann = new MemoryStoreRiemann();

        [Fact]
        public async Task RunAllScenarios()
        {
            ConfigProvider.UseProvider(_testConfigProvider);
            InitializeLogging();
            CommandContext.Riemann = _testRiemann;

            File.Delete(ReportFile);

            // Don't do these in a Task.WhenAll() - it'll run them in parallel, and (for now) we'd like them to be serial
            // to avoid overloading a single scenario, and also because we can only run one HTTP server on 22222 at once.
            var sets = new List<ChartSet>
            {
                await IdealInheritedCommand(),
                await IdealCommandAttribute(),
                await DelayedSuccess(),
                await FastFailures(),
            };

            var output = @"<!doctype html>
<html>
  <head>
    <script src='http://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js'></script>
    <script src='http://cdnjs.cloudflare.com/ajax/libs/highcharts/4.0.1/highcharts.js'></script>
    <style type='text/css'>
html, body { margin: 0; padding: 0; font-family: Tahoma, Arial, sans-serif; background-color: #676B85; }
h2 { margin: 0px; padding: 0; }
    </style>
  </head>
  <body>
    <div style='white-space: nowrap;'>
";
            var count = 0;

            foreach (var set in sets)
            {
                output += "<div style='width: 500px; border-right: 2px solid #5B5D73; background-color: #676B85; color: #fff; display: inline-block; vertical-align: top; padding: 5px;'><h2 style='white-space: nowrap; overflow: hidden;'>" + set.Name + "</h2><div style='white-space: normal; height: 130px; overflow: hidden;'>" + set.Description + "</div>";

                foreach (var chart in set.Charts)
                {
                    var id = "chart" + count;
                    output += string.Format(@"<div style='background-color: #ffffff; margin-bottom: 5px;'>
<div id='{0}' style='height: 150px;'></div>
<script type='text/javascript'>$('#{1}').highcharts({2});</script>
</div>", id, id, JObject.FromObject(chart.HighchartsOptions));

                    count++;
                }

                output += "</div>";
            }

            output += @"</div></body>
</html>
";

            File.WriteAllText(ReportFile, output);
        }

        private IEnumerable<Task> Repeat(int perSecond, int seconds, Func<Task<HttpStatusCode>> execute)
        {
            for (var i = 0; i < (perSecond * seconds); i++)
            {
                yield return execute();
                Thread.Sleep(1000 / perSecond);
            }
        }

        // I've made a couple passes at trying to refactor out the scenarios, but haven't
        // come across one that feels right. My leaning right now is to separate server behavior
        // and client behavior, and provide a couple config objects to each. That way we could
        // define somewhat generic server and client behavior and mix/match them.
        //
        // I think we need to write about 8-10 different scenarios with different configurations
        // and server behavior to understand how to refactor these in a way that makes sense.
        //
        // Versaw suggested using observables, which sound fitting.
        // http://msdn.microsoft.com/en-us/library/hh242977(v=vs.103).aspx
        //
        // But for now ... copypasta.

        private async Task<ChartSet> IdealInheritedCommand()
        {
            const string key = "system-test-1";
            

            _testConfigProvider.Set("mjolnir.breaker.system-test-1.minimumOperations", 5);
            _testConfigProvider.Set("mjolnir.breaker.system-test-1.thresholdPercentage", 50);
            _testConfigProvider.Set("mjolnir.breaker.system-test-1.trippedDurationMillis", 10000);
            _testConfigProvider.Set("mjolnir.metrics.system-test-1.windowMillis", 10000);

            using (var server = new HttpServer(1))
            {
                var url = string.Format("http://localhost:{0}/", ServerPort);

                server.Start(ServerPort);
                server.ProcessRequest += ServerBehavior.Immediate200();

                _testRiemann.ClearAndStart();

                await Task.WhenAll(Repeat(10, 30, () =>
                {
                    var command = new HttpClientCommand(key, url, TimeSpan.FromSeconds(10));
                    return command.InvokeAsync();
                }));

                server.Stop();
            }

            _testRiemann.Stop();

            File.WriteAllLines(string.Format(@"c:\hudl\logs\mjolnir-metrics-{0}-{1}.txt", key, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")), _testRiemann.Metrics.Select(m => m.ToCsvLine()));

            return new ChartSet
            {
                Name = "Ideal (Inherited Command)",
                Description = "30s @ 5/sec.<br/>Command: Inherited<br/>Timeout: 10000<br/>Server: Immediate 200<br/>Breaker: 50% / 10sec, min 5 ops, 10s window",
                Charts = GatherChartData(_testRiemann.Metrics, key, key + ".HttpClient"),
            };
        }

        private async Task<ChartSet> IdealCommandAttribute()
        {
            const string key = "system-test-4"; // Keep this matched up with the attribute on IHttpClientService

            _testConfigProvider.Set("mjolnir.breaker.system-test-4.minimumOperations", 5);
            _testConfigProvider.Set("mjolnir.breaker.system-test-4.thresholdPercentage", 50);
            _testConfigProvider.Set("mjolnir.breaker.system-test-4.trippedDurationMillis", 10000);
            _testConfigProvider.Set("mjolnir.metrics.system-test-4.windowMillis", 10000);

            // Command timeout is defined on the interface.
            var instance = new HttpClientService();
            var proxy = CommandInterceptor.CreateProxy<IHttpClientService>(instance);

            using (var server = new HttpServer(1))
            {
                var url = string.Format("http://localhost:{0}/", ServerPort);

                server.Start(ServerPort);
                server.ProcessRequest += ServerBehavior.Immediate200();

                _testRiemann.ClearAndStart();

                await Task.WhenAll(Repeat(10, 30, () => proxy.MakeRequest(url, CancellationToken.None)));

                server.Stop();
            }

            _testRiemann.Stop();

            File.WriteAllLines(string.Format(@"c:\hudl\logs\mjolnir-metrics-{0}-{1}.txt", key, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")), _testRiemann.Metrics.Select(m => m.ToCsvLine()));

            return new ChartSet
            {
                Name = "Ideal (Command Attribute)",
                Description = "30s @ 5/sec.<br/>Command: Inherited<br/>Timeout: 10000<br/>Server: Immediate 200<br/>Breaker: 50% / 10sec, min 5 ops, 10s window",
                Charts = GatherChartData(_testRiemann.Metrics, key, key + ".IHttpClientService-MakeRequest"),
            };
        }

        private async Task<ChartSet> DelayedSuccess()
        {
            const string key = "system-test-2";

            _testConfigProvider.Set("mjolnir.breaker.system-test-2.minimumOperations", 5);
            _testConfigProvider.Set("mjolnir.breaker.system-test-2.thresholdPercentage", 50);
            _testConfigProvider.Set("mjolnir.breaker.system-test-2.trippedDurationMillis", 10000);
            _testConfigProvider.Set("mjolnir.metrics.system-test-2.windowMillis", 10000);

            using (var server = new HttpServer(15))
            {
                var url = string.Format("http://localhost:{0}/", ServerPort);

                server.Start(ServerPort);
                server.ProcessRequest += ServerBehavior.Delayed200(TimeSpan.FromMilliseconds(15000));

                _testRiemann.ClearAndStart();

                await Task.WhenAll(Repeat(1, 30, () =>
                {
                    var command = new HttpClientCommand(key, url, TimeSpan.FromSeconds(30));
                    return command.InvokeAsync();
                }));

                server.Stop();
            }

            _testRiemann.Stop();

            File.WriteAllLines(string.Format(@"c:\hudl\logs\mjolnir-metrics-{0}-{1}.txt", key, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")), _testRiemann.Metrics.Select(m => m.ToCsvLine()));

            return new ChartSet
            {
                Name = "Slow Success",
                Description = "30s @ 5/sec.<br/>Command: Inherited<br/>Timeout: 30000<br/>Server: Delayed (15s) 200<br/>Breaker: 50% / 10sec, min 5 ops, 10s window",
                Charts = GatherChartData(_testRiemann.Metrics, key, key + ".HttpClient"),
            };
        }

        private async Task<ChartSet> FastFailures()
        {
            const string key = "system-test-3";

            _testConfigProvider.Set("mjolnir.breaker.system-test-3.minimumOperations", 5);
            _testConfigProvider.Set("mjolnir.breaker.system-test-3.thresholdPercentage", 50);
            _testConfigProvider.Set("mjolnir.breaker.system-test-3.trippedDurationMillis", 10000);
            _testConfigProvider.Set("mjolnir.metrics.system-test-3.windowMillis", 10000);

            using (var server = new HttpServer(1))
            {
                var url = string.Format("http://localhost:{0}/", ServerPort);

                server.Start(ServerPort);
                server.ProcessRequest += ServerBehavior.Immediate500();

                _testRiemann.ClearAndStart();

                await Task.WhenAll(Repeat(5, 30, async () =>
                {
                    var command = new HttpClientCommand(key, url, TimeSpan.FromSeconds(30));

                    try
                    {
                        return await command.InvokeAsync();
                    }
                    catch (Exception)
                    {
                        return HttpStatusCode.InternalServerError;
                    }
                }));

                server.Stop();
            }

            _testRiemann.Stop();

            File.WriteAllLines(string.Format(@"c:\hudl\logs\mjolnir-metrics-{0}-{1}.txt", key, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")), _testRiemann.Metrics.Select(m => m.ToCsvLine()));

            return new ChartSet
            {
                Name = "Fast Failures",
                Description = "30s @ 5/sec.<br/>Command: Inherited<br/>Timeout: 30000<br/>Server: Immediate 500<br/>Breaker: 50% / 10sec, min 5 ops, 10s window",
                Charts = GatherChartData(_testRiemann.Metrics, key, key + ".HttpClient"),
            };
        }

        private static void InitializeLogging()
        {
            var appender = new FileAppender
            {
                Threshold = Level.Debug,
                AppendToFile = true,
                File = @"c:\hudl\logs\mjolnir-system-test-log.txt",
                Layout = new PatternLayout("%utcdate %property{log4net:HostName} [%-5level] [%logger] %message%newline"),
                LockingModel = new FileAppender.MinimalLock(),
            };
            appender.ActivateOptions();

            BasicConfigurator.Configure(appender);
        }

        private List<Chart> GatherChartData(List<Metric> metrics, string key, string commandName)
        {
            return new List<Chart>
            {
                Chart.Create("Circuit breaker state", new List<object>
                {
                    new
                    {
                        name = "state (open/closed)",
                        data = metrics
                            .Where(m => m.Service == "mjolnir breaker " + key + " IsAllowing")
                            .Select(m => new
                            {
                                x = m.OffsetSeconds,
                                y = (m.Status == "Allowed" ? 1 : 0),
                                color = (m.Status == "Allowed" ? "#00FF00" : "#FF0000"),
                            })
                            .ToArray(),
                        color = "#CCCCCC",
                    }
                }),
                Chart.Create("InvokeAsync() elapsed ms + result", new List<object>
                {
                    new
                    {
                        name = "elapsed (ms)",
                        data = metrics
                            .Where(m => m.Service == "mjolnir command " + commandName + " InvokeAsync")
                            .Select(m => new
                            {
                                x = m.OffsetSeconds,
                                y = m.Value,
                                color = GetColorForCommandStatus(m.Status),
                            }),
                    }
                }),
                Chart.Create("Thread pool use", new List<object>
                {
                    new
                    {
                        name = "active",
                        data = metrics
                            .Where(m => m.Service == "mjolnir pool " + key + " activeThreads")
                            .Select(m => new object[] { m.OffsetSeconds, m.Value })
                            .ToArray(),
                    },
                    new
                    {
                        name = "in use",
                        data = metrics
                            .Where(m => m.Service == "mjolnir pool " + key + " inUseThreads")
                            .Select(m => new object[] { m.OffsetSeconds, m.Value })
                            .ToArray(),
                    }
                }),
                Chart.Create(metrics, "Breaker total observed operations", "total", "mjolnir breaker " + key + " total"),
                Chart.Create(metrics, "Breaker observed error percent", "error %", "mjolnir breaker " + key + " error"),
            };
        }

        private static string GetColorForCommandStatus(string status)
        {
            switch (status)
            {
                case "RanToCompletion":
                    return "#00FF00";
                case "Faulted":
                    return "#FF0000";
                case "Canceled":
                    return "#FFFF00";
                case "Rejected":
                    return "#CCCC00";
                default:
                    return "#000000";
            }
        }
    }
}
