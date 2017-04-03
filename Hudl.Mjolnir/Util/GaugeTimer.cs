using System.Timers;

namespace Hudl.Mjolnir.Util
{
    internal class GaugeTimer
    {
        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        // Don't let these get garbage collected.
        private readonly Timer _timer;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        /// <summary>
        /// Constructs a new GaugeTimer that invokes the provided handler.
        /// </summary>
        /// <param name="onTick">Event handler to invoke on tick</param>
        internal GaugeTimer(ElapsedEventHandler onTick)
        {
            _timer = new Timer(1000) { AutoReset = true };
            _timer.Elapsed += onTick;
            _timer.Enabled = true;
        }
    }
}
