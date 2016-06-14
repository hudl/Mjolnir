**Hey!** Use **`IMetricEvents`** instead. The API is clearer and more targeted at Mjolnir behavior (and less targeted at method profiling).

=====

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

Each of these send a millisecond value for their metric.

```
service                                    possible states                                   description
--------------------------------------------------------------------------------------------------------

// These three are the most useful, and can help profile your application's Commands.

mjolnir command [name] execute             RanToCompletion | Canceled | Rejected | Faulted   ExecuteAsync() elapsed
mjolnir command [name] fallback            Success | Rejected | NotImplemented | Failure     Fallback() elapsed
mjolnir command [name] total               RanToCompletion | Canceled | Rejected | Faulted   ExecuteAsync() + Fallback() elapsed

// These are mainly for internal Mjolnir profiling, and may be removed in the future.

mjolnir breaker [key] IsAllowing           Allowed | Rejected
mjolnir breaker [key] AllowSingleTest      Unknown | MissedLock | Allowed | NotEligible
mjolnir breaker [key] CheckAndSetTripped   Unknown | AlreadyTripped | MissedLock | CriteriaNotMet | JustTripped
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
mjolnir fallback-semaphore [key]           Full | Available   semaphore available spots (0 = full, will reject)
mjolnir pool [key] activeThreads           null               active thread count
mjolnir pool [key] inUseThreads            null               in use thread count
mjolnir pool [key] pendingCompletion       null               total incomplete work item count (pool + queue)
```