using System.Threading;

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
        internal GaugeTimer(TimerCallback onTick)
        {
            _timer = new Timer(onTick, null, 1000, 1000);
        }
    }
}
