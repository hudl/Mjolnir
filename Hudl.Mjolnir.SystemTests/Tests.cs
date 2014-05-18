using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using Hudl.Common.Extensions;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Riemann;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
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
                    Description = "10 operations, 1/second. Endpoint delays 15 seconds and then returns 200. Command timeouts are 10 seconds.",
                    Charts = await RunScenario2(),
                },
                new ChartSet
                {
                    Name = "Fast Failures",
                    Description = "150 operations, 5/second. Endpoint immediately returns 500.",
                    Charts = await RunScenario3(),
                },
            };

            var output = @"<!doctype html>
<html>
  <head>
    <script src='http://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js'></script>
    <script src='http://cdnjs.cloudflare.com/ajax/libs/highcharts/4.0.1/highcharts.js'></script>
    <style type='text/css'>
html, body { margin: 0; padding: 0; }
    </style>
  </head>
  <body>
    <div style='white-space: nowrap;'>
";
            var count = 0;

            foreach (var set in sets)
            {
                output += "<div style='width: 500px; display: inline-block;'><h2>" + set.Name + "</h2><div style='white-space: normal;'>" + set.Description + "</div>";

                foreach (var chart in set.Charts)
                {
                    var id = "chart" + count;
                    output += string.Format(@"<div>
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

    internal class ChartSet
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Chart> Charts { get; set; } 
    }

    internal class Chart
    {
        public object HighchartsOptions { get; set; }

        public static Chart Create(List<Metric> metrics, string title, string seriesLabel, string service)
        {
            return Create(metrics, title, seriesLabel, service, m => m.Value);
        }

        public static Chart Create(List<Metric> metrics, string title, string seriesLabel, string service, Func<Metric, float?> valueSelector)
        {
            var series = new List<object>
            {
                new
                {
                    data = metrics
                        .Where(m => m.Service == service)
                        .Select(m => new object[] { m.OffsetSeconds, valueSelector(m) })
                        .ToArray(),
                    name = seriesLabel,
                },
            };
            return Create(title, series);
        }

        public static Chart Create(string title, List<object> series)
        {
            return new Chart
            {
                HighchartsOptions = new
                {
                    chart = new
                    {
                        marginLeft = 50,
                        type = "scatter",
                    },
                    title = new
                    {
                        text = title,
                        align = "left",
                        style = "color: #333333; fontSize: 12px;"
                    },
                    legend = new
                    {
                        enabled = true,
                        verticalAlign = "top",
                        align = "right",
                        floating = true,
                    },
                    yAxis = new
                    {
                        title = (string)null,
                        //categories = new [] {"open", "closed"},
                        min = 0,
                    },
                    series = series,
                }
            };
        }
    }

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
            var status = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return status;
            }
            
            throw new Exception("Status " + status);
        }
    }

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

    // From http://stackoverflow.com/a/4673210/29995
    internal class HttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;

        public HttpServer(int maxThreads)
        {
            _workers = new Thread[maxThreads];
            _queue = new Queue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }

        public void Start(int port)
        {
            _listener.Prefixes.Add(String.Format(@"http://+:{0}/", port));
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _stop.Set();
            _listenerThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                var context = _listener.BeginGetContext(ContextReady, null);

                if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                    return;
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch
            {
                return;
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try
                {
                    ProcessRequest(context);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        public event Action<HttpListenerContext> ProcessRequest;
    }

    internal class SystemTestConfigProvider : IConfigurationProvider
    {
        private static readonly Dictionary<string, object> Values = new Dictionary<string, object>
        {
            { "mjolnir.useCircuitBreakers", true },
            //{ "stats.riemann.isEnabled", false },
            //{ "command.system-test.HttpClient.Timeout", 15000 },
            { "mjolnir.gaugeIntervalMillis", 500 },
            
            //{ "mjolnir.pools.system-test.threadCount", 10 },
            //{ "mjolnir.pools.system-test.queueLength", 10 },
            /*{ "", false },
            { "", false },
            { "", false },
            { "", false },
            { "", false },
            { "", false },
            { "", false },*/
        }; 

        public T Get<T>(string configKey)
        {
            return ConvertValue<T>(Values.ContainsKey(configKey) ? Values[configKey] : null);
        }

        public object Get(string configKey)
        {
            return Values.ContainsKey(configKey) ? Values[configKey] : null;
        }

        public void Set(string configKey, object value)
        {
            throw new NotImplementedException();
        }

        public void Delete(string configKey)
        {
            throw new NotImplementedException();
        }

        public string[] GetKeys(string prefix)
        {
            return Values.Keys.ToArray();
        }

        public T ConvertValue<T>(object value)
        {
            return DefaultValueConverter.ConvertValue<T>(value);
        }

        public event ConfigurationChangedHandler ConfigurationChanged;
    }

    internal class MemoryStoreRiemann : IRiemann
    {
        //private static readonly ILog Log = LogManager.GetLogger("metrics");
        //private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Normally you'll want to .Stop() before accessing this.
        public List<Metric> Metrics
        {
            get
            {
                lock (_lock)
                {
                    return new List<Metric>(_metrics);    
                }
            }
        }

        private readonly object _lock = new object();
        private readonly List<Metric> _metrics = new List<Metric>();

        private DateTime _startTime = DateTime.UtcNow;
        private bool _isEnabled = true;

        public void Stop()
        {
            _isEnabled = false;
        }

        public void ClearAndStart()
        {
            _metrics.Clear();
            _startTime = DateTime.UtcNow;
            _isEnabled = true;
        }

        private double OffsetMillis()
        {
            return (DateTime.UtcNow - _startTime).TotalMilliseconds;
        }

        private void Store(string service, string state, object metric)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                var m = (metric == null ? (float?) null : float.Parse(metric.ToString()));
                _metrics.Add(new Metric(OffsetMillis() / 1000, service, state, m));
            }
        }

        public void Event(string service, string state, long? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void Event(string service, string state, float? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void Event(string service, string state, double? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void Elapsed(string service, string state, TimeSpan elapsed, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, elapsed.TotalMilliseconds);
        }

        public void Gauge(string service, string state, long? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void ConfigGauge(string service, long metric)
        {
            Store(service, null, metric);
        }
    }

    internal class Metric
    {
        public double OffsetSeconds { get; private set; }
        public string Service { get; private set; }
        public string Status { get; private set; }
        public float? Value { get; private set; }

        public Metric(double offsetSeconds, string service, string status, float? value)
        {
            OffsetSeconds = offsetSeconds;
            Service = service;
            Status = status;
            Value = value;
        }
    }
}
