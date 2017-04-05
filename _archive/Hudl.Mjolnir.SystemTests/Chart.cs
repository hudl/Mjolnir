using System;
using System.Collections.Generic;
using System.Linq;

namespace Hudl.Mjolnir.SystemTests
{
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
                        backgroundColor = (string) null,
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
                    xAxis = new
                    {
                        min = 0,
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
}