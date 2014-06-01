## Events sent to `IStats` methods

### Calls to `Event()`

None of these actually send metric values, they just indicate method results and state changes.

```
service                                    possible states                    metric     sent when
--------------------------------------------------------------------------------------------------
mjolnir breaker [key] MarkSuccess          Ignored | Fixed                    null       any command succeeds and reports success to the breaker
mjolnir breaker [key]                      Tripped                            null       the breaker trips
mjolnir metrics [key] Mark                 CommandSuccess | CommandFailure    null       a command is marked as a success or failure with the breaker
mjolnir pool [key] thread                  Initialized | Terminated           null       a thread pool thread is created
```

### Calls to `Elapsed()`

Each of these send a millisecond value for their metric. These are mostly for profiling, and many will likely be removed in the future.

The three significant metrics are:
- `mjolnir command [name] InvokeAsync` - the entire command + fallback
- `mjolnir command [name] ExecuteInIsolation` - the command only (without fallback)
- `mjolnir command [name] TryFallback` - the command's fallback execution

```
service                                    possible states
----------------------------------------------------------
mjolnir breaker [key] IsAllowing           Allowed | Rejected
mjolnir breaker [key] AllowSingleTest      Unknown | MissedLock | Allowed | NotEligible
mjolnir breaker [key] CheckAndSetTripped   Unknown | AlreadyTripped | MissedLock | CriteriaNotMet | JustTripped
mjolnir command [name] ExecuteInIsolation  RanToCompletion | Canceled | Rejected | Faulted
mjolnir command [name] InvokeAsync         RanToCompletion | Canceled | Rejected | Faulted
mjolnir command [name] TryFallback         Success | Rejected | NotImplemented | Failure
mjolnir metrics [key] CreateSnapshot       null
mjolnir metrics [key] GetSnapshot          null
mjolnir metrics [key] Reset                null
mjolnir pool [key] Start                   null
mjolnir pool [key] Enqueue                 Rejected | Enqueued
```

### Calls to `Gauge()`

These are periodic gauges for the state of various components - they fire every five seconds and aren't tied to any particular event.

```
service                                    possible states    description
-------------------------------------------------------------------------
mjolnir breaker [key] total                Above | Below      total breaker operation count in current window (0+)
mjolnir breaker [key] error                Above | Below      error percentage in current window (0-100)
mjolnir context breakers                   null               number of active circuit breakers
mjolnir context metrics                    null               number of active metrics counter instances
mjolnir context pools                      null               number of active thread pools
mjolnir context semaphores                 null               number of active fallback semaphores
mjolnir fallback-semaphore [key]           Full | Available   semaphore available spots (0 = full, will reject)
mjolnir pool [key] activeThreads           null               active thread count
mjolnir pool [key] inUseThreads            null               in use thread count
mjolnir pool [key] pendingCompletion       null               total incomplete work item count (pool + queue)
```