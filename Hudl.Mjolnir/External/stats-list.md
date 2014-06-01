## Events sent to `IStats` methods

### Calls to `Event()`

None of these actually send metric values, they just indicate method results and state changes.

```
              service                                    possible states                    metric     sent when
----------------------------------------------------------------------------------------------------------------
event_l       mjolnir breaker [key] MarkSuccess          Ignored | Fixed                    null       any command succeeds and reports success to the breaker
event_l       mjolnir breaker [key]                      Tripped                            null       the breaker trips
event_l       mjolnir metrics [key] Mark                 CommandSuccess | CommandFailure    null       a command is marked as a success or failure with the breaker
event_l       mjolnir pool [key] thread                  Initialized | Terminated           null       a thread pool thread is created
```

### Calls to `Elapsed()`

Each of these send a millisecond value for their metric. These are mostly for profiling, and many will likely be removed in the future.

The three significant metrics are:
- `mjolnir command [name] InvokeAsync` - elapsed milliseconds for the entire command + fallback
- `mjolnir command [name] ExecuteInIsolation` - elapsed milliseconds for the command only (without fallback)
- `mjolnir command [name] TryFallback` - elapsed milliseconds for the command's fallback execution

```
              service                                    possible states
------------------------------------------------------------------------
elapsed       mjolnir breaker [key] IsAllowing           Allowed | Rejected
elapsed       mjolnir breaker [key] AllowSingleTest      Unknown | MissedLock | Allowed | NotEligible
elapsed       mjolnir breaker [key] CheckAndSetTripped   Unknown | AlreadyTripped | MissedLock | CriteriaNotMet | JustTripped
elapsed       mjolnir command [name] ExecuteInIsolation  RanToCompletion | Canceled | Rejected | Faulted
elapsed       mjolnir command [name] InvokeAsync         RanToCompletion | Canceled | Rejected | Faulted
elapsed       mjolnir command [name] TryFallback         Success | Rejected | NotImplemented | Failure
elapsed       mjolnir metrics [key] CreateSnapshot       null
elapsed       mjolnir metrics [key] GetSnapshot          null
elapsed       mjolnir metrics [key] Reset                null
elapsed       mjolnir pool [key] Start                   null
elapsed       mjolnir pool [key] Enqueue                 Rejected | Enqueued
```

### Calls to `Gauge()`

These are periodic gauges for the state of various components - they fire every five seconds and aren't tied to any particular event.

```
              service                                    possible states    description
---------------------------------------------------------------------------------------
gauge         mjolnir breaker [key] total                Above | Below      total breaker operation count in current window (0+)
gauge         mjolnir breaker [key] error                Above | Below      error percentage in current window (0-100)
gauge         mjolnir context breakers                   null               number of active circuit breakers
gauge         mjolnir context metrics                    null               number of active metrics counter instances
gauge         mjolnir context pools                      null               number of active thread pools
gauge         mjolnir context semaphores                 null               number of active fallback semaphores
gauge         mjolnir fallback-semaphore [key]           Full | Available   semaphore available spots (0 = full, will reject)
gauge         mjolnir pool [key] activeThreads           null               active thread count
gauge         mjolnir pool [key] inUseThreads            null               in use thread count
gauge         mjolnir pool [key] pendingCompletion       null               total incomplete work item count (pool + queue)
```