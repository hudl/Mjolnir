using System.Timers;
using Hudl.Config;

namespace Hudl.Mjolnir.Util
{
    internal class GaugeTimer
    {
        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        // Don't let these get garbage collected.
        private readonly Timer _timer;
        private readonly ConfigurableValue<long> _gaugeIntervalMillis;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        public GaugeTimer(ElapsedEventHandler onTick)
        {
            _gaugeIntervalMillis = new ConfigurableValue<long>("mjolnir.gaugeIntervalMillis", 5000, UpdateStatsGaugeInterval);

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
