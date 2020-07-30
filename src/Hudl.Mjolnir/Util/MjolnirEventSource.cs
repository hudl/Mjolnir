using System;
using System.Diagnostics.Tracing;

namespace Hudl.Mjolnir.Util
{
    [EventSource(Name = "Mjolnir")]
    class MjolnirEventSource : EventSource
    {
        public static MjolnirEventSource Log = new MjolnirEventSource();

        public class Keywords
        {
            public const EventKeywords CircuitBreaker = (EventKeywords)1;
            public const EventKeywords Bulkhead = (EventKeywords)2;
            public const EventKeywords Command = (EventKeywords)3;

        }

        public class Tasks
        {
            public const EventTask CircuitBreaker = (EventTask)1;
            public const EventTask Bulkhead = (EventTask)2;
            public const EventTask Command = (EventTask)2;
        }

        [Event(1, Opcode = EventOpcode.Start, Task = Tasks.CircuitBreaker, Keywords = Keywords.CircuitBreaker, Level = EventLevel.Informational)]
        public void CircuitBreakerEntered(string name) { if (IsEnabled()) WriteEvent(1, name); }

        [Event(2, Opcode = EventOpcode.Stop, Task = Tasks.CircuitBreaker, Keywords = Keywords.CircuitBreaker, Level = EventLevel.Informational)]
        public void CircuitBreakerExited(string name) { if (IsEnabled()) WriteEvent(2, name); }

        [Event(3, Opcode = EventOpcode.Resume, Task = Tasks.CircuitBreaker, Keywords = Keywords.CircuitBreaker, Level = EventLevel.Informational)]
        public void CircuitBreakerFixed(string name) { if (IsEnabled()) WriteEvent(3, name); }

        [Event(4, Message = "Circut Breaker Tripped {0}", Opcode = EventOpcode.Suspend, Task = Tasks.CircuitBreaker, Keywords = Keywords.CircuitBreaker, Level = EventLevel.Informational)]
        public void CircuitBreakerTripped(string name) { if (IsEnabled()) WriteEvent(4, name); }

        [Event(5, Opcode = EventOpcode.Info, Task = Tasks.CircuitBreaker, Keywords = Keywords.CircuitBreaker, Level = EventLevel.Informational)]
        public void CircuitBreakerTest(string name) { if (IsEnabled()) WriteEvent(5, name); }

        [Event(6, Opcode = EventOpcode.Info, Task = Tasks.CircuitBreaker, Keywords = Keywords.CircuitBreaker, Level = EventLevel.Informational)]
        public void CircuitBreakerRejection(string breaker, string command)
        {
            if (IsEnabled()) WriteEvent(6, breaker, command);
        }

        [Event(7, Message = "Invoking Command {0}", Opcode = EventOpcode.Start, Task = Tasks.Command, Keywords = Keywords.Command, Level = EventLevel.Informational)]
        public void CommandInvoked(string command)
        {
            if (IsEnabled()) WriteEvent(7, command);
        }

        [Event(8, Opcode = EventOpcode.Stop, Task = Tasks.Command, Keywords = Keywords.Command, Level = EventLevel.Informational)]

        public void CommandSuccess(string command)
        {
            if (IsEnabled()) WriteEvent(8, command);
        }

        [Event(9, Opcode = EventOpcode.Stop, Task = Tasks.Command, Keywords = Keywords.Command, Level = EventLevel.Informational)]

        public void CommandFailure(string command)
        {
            if (IsEnabled()) WriteEvent(9, command);
        }
    }
}
