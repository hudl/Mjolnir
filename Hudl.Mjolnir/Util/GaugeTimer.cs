using System.Timers;
using Hudl.Config;

namespace Hudl.Mjolnir.Util
{
    internal class GaugeTimer
    {
        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        // Don't let these get garbage collected.
        private readonly Timer _timer;
        private readonly IConfigurableValue<long> _gaugeIntervalMillis;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        /// <summary>
        /// Constructs a new GaugeTimer that invokes the provided handler.
        /// 
        /// If the interval millis override is provided, it'll be used. Otherwise a
        /// ConfigurableValue for "mjolnir.gaugeIntervalMillis" || 5000 will be used.
        /// 
        /// intervalMillisOverride should typically only be used for testing.
        /// </summary>
        /// <param name="onTick">Event handler to invoke on tick</param>
        /// <param name="intervalMillisOverride">Interval override (for unit testing)</param>
        internal GaugeTimer(ElapsedEventHandler onTick, IConfigurableValue<long> intervalMillisOverride = null)
        {
            _gaugeIntervalMillis = intervalMillisOverride ?? new ConfigurableValue<long>("mjolnir.gaugeIntervalMillis", 5000, UpdateStatsGaugeInterval);

            _timer = new Timer(_gaugeIntervalMillis.Value) { AutoReset = true };
            _timer.Elapsed += onTick;
            _timer.Enabled = true;
        }

        private void UpdateStatsGaugeInterval(long millis)
        {
            if (_timer == null)
            {
                return;
            }
            _timer.Interval = millis;
        }
    }
}
