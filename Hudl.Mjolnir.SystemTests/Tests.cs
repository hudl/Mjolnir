using System.IO;
using System.Linq;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

            var sets = new List<ChartSet>
            {
                new ChartSet
                {
                    Name = "Ideal",
                    Description = "10 operations, 1/second. Endpoint immediately returns 200",
                    Charts = await RunScenario(),
                },
                new ChartSet
                {
                    Name = "Slow Success",
                    Description = "10 operations, 1/second. Endpoint delays 15 seconds and then returns 200. Command timeouts are 30 seconds.",
                    Charts = await RunScenario2(),
                },
                new ChartSet
                {
                    Name = "Fast Failures",
                    Description = "150 operations, 5/second. Endpoint immediately returns 500. Breaker/pool/metrics are using default config values.",
                    Charts = await RunScenario3(),
                },
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
                output += "<div style='width: 500px; border-right: 2px solid #5B5D73; background-color: #676B85; color: #fff; display: inline-block; vertical-align: top; padding: 5px;'><h2 style='white-space: nowrap; overflow: hidden;'>" + set.Name + "</h2><div style='white-space: normal; height: 100px; overflow: hidden;'>" + set.Description + "</div>";

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

        private async Task<List<Chart>> RunScenario()
        {
            const string key = "system-test-1";

            using (var server = new HttpServer(1))
            {
                server.Start(ServerPort);
                server.ProcessRequest += ServerBehavior.Immediate200();

                _testRiemann.ClearAndStart();

                var tasks = new List<Task>();

                for(var i = 0; i < 10; i++)
                {

                    var url = string.Format("http://localhost:{0}/", ServerPort);
                    var command = new HttpClientCommand(key, url, TimeSpan.FromSeconds(10));

                    tasks.Add(command.InvokeAsync());

                    Thread.Sleep(1000);
                }

                await Task.WhenAll(tasks);

                server.Stop();
            }

            _testRiemann.Stop();

            return GatherChartData(_testRiemann.Metrics, key);
        }

        private async Task<List<Chart>> RunScenario2()
        {
            const string key = "system-test-2";

            using (var server = new HttpServer(15))
            {
                server.Start(ServerPort);
                server.ProcessRequest += ServerBehavior.Delayed200(TimeSpan.FromMilliseconds(15000));

                _testRiemann.ClearAndStart();

                var tasks = new List<Task>();

                for (var i = 0; i < 10; i++)
                {
                    var url = string.Format("http://localhost:{0}/", ServerPort);
                    var command = new HttpClientCommand(key, url, TimeSpan.FromSeconds(30));

                    tasks.Add(command.InvokeAsync());

                    Thread.Sleep(1000);
                }

                await Task.WhenAll(tasks);

                server.Stop();
            }

            _testRiemann.Stop();

            return GatherChartData(_testRiemann.Metrics, key);
        }

        private async Task<List<Chart>> RunScenario3()
        {
            const string key = "system-test-3";

            using (var server = new HttpServer(1))
            {
                server.Start(ServerPort);
                server.ProcessRequest += ServerBehavior.Immediate500();

                _testRiemann.ClearAndStart();

                for (var i = 0; i < 150; i++)
                {
                    var url = string.Format("http://localhost:{0}/", ServerPort);
                    var command = new HttpClientCommand(key, url, TimeSpan.FromSeconds(30));

                    try
                    {
                        await command.InvokeAsync();
                    }
                    catch (Exception)
                    {
                        // Expected.
                    }

                    Thread.Sleep(200);
                }

                server.Stop();
            }

            _testRiemann.Stop();

            File.WriteAllLines(string.Format(@"c:\hudl\logs\mjolnir-metrics-{0}-{1}.txt", key, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")), _testRiemann.Metrics.Select(m => m.ToCsvLine()));
            return GatherChartData(_testRiemann.Metrics, key);
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

        private List<Chart> GatherChartData(List<Metric> metrics, string key)
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
                            .Where(m => m.Service == "mjolnir command " + key + ".HttpClient InvokeAsync")
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
