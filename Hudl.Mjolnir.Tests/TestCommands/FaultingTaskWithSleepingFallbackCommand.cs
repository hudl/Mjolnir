using System;
using System.Threading;
using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    class FaultingTaskWithSleepingFallbackCommand : FaultingTaskWithoutFallbackCommand
    {
        private readonly TimeSpan _sleepDuration;

        internal FaultingTaskWithSleepingFallbackCommand(TimeSpan sleepDuration)
        {
            _sleepDuration = sleepDuration;
        }

        protected override object Fallback(CommandFailedException instigator)
        {
            Thread.Sleep(_sleepDuration);
            return new { };
        }
    }
}
