Mjolnir
=======

When bad things happen, Mjolnir helps
- isolate them from the rest of the application/system
- shed load from failing downstream dependencies
- fail fast back to the caller

Modeled after Netflix's [Hystrix](https://github.com/Netflix/Hystrix) library.

Commands
-----

Commands are the heart of Mjolnir, and are how you use its protections. You can either:
- extend `Command<TResult>` and put dangerous code in its `ExecuteAsync()` method, or
- add `[Command]` to an `interface`, which wraps all of its methods in a Command.

**Extend `Command<TResult>`**

*Example*

```csharp
public class GetTeamCommand : Command<TeamDto>
{
    private readonly string _teamId;

    public UpdateTeamCommand(string teamId)
        : base("core-client", "core-team", TimeSpan.FromMilliseconds(15000)
    {
        _teamId = teamId;
    }
    
    protected override Task<TeamDto> ExecuteAsync(CancellationToken token)
    {
        // Go over the network to query the TeamDto using the id.
    }
}
```

*Constructor*

`Command` needs to be constructed with a few required values. Here are the `base` constructor signatures:

```csharp
// group: A logical grouping for the command. Commands within the same client package or collection of commands typically get grouped together.
// breakerKey: The named circuit breaker to use for the Command.
// poolKey: The named thread pool to use for the Command.
// defaultTimeout: If not overridden via config, the timeout after which the Command will be cancelled.
Command(string group, string breakerKey, string poolKey, TimeSpan defaultTimeout)

// isolationKey: Sets both the breakerKey and poolKey.
Command(string group, string isolationKey, TimeSpan defaultTimeout)
```

For more information on the keys, see [Circuit Breakers](#circuit-breakers) and [Thread Pools](#thread-pools).

**`[Command]` attribute**

*Example*

```csharp
[Command("core-client", "core-team", 15000)]
public interface ITeamService
{
    TeamDto GetTeam(string teamId, CancellationToken? token = null);
    void UpdateTeam(TeamDto teamDto, CancellationToken? token = null);
}

public class TeamService : ITeamService { /* implementation */ }

// ...

void Main() {
    // This is typically done in a service locator of some sort and cached.
    var proxy = CommandInterceptor.CreateProxy<ITeamService>(new TeamService());
    var teamDto = proxy.GetTeam("1234");
}
```

Using `[Command]` provides the same benefits as extending `Command<TResult>`, but can be more convenient at times.

Note the presence of `CancellationToken`s on the interface methods. If your interface method signature contains a `CancellationToken` (which is optional), Mjolnir will pass a token created from the timeout through to your method, allowing you to cooperatively cancel your operation. If no `CancellationToken` parameter is present, the Command timeout may be less effective. Mjolnir will only pass its token through if it doesn't already see a non-null or non-empty token as the parameter value.

Thread Pools
-----

Thread pools help guard against one type of operation consuming more than its share of resources and starving out other operations.

*Example:*

Imagine an operation that makes a network call to a different cluster. If that downstream cluster becomes very slow, our calls will start blocking and waiting for responses.

Under high enough traffic volume, our cluster will start building up pending operations that are waiting for the downstream cluster to respond, which means they're taking up increasingly more threads - potentially as many as they can - leaving fewer threads for normal, succeeding operatiosn to work with.

To prevent this, Commands are grouped into thread pools. Each thread pool receives a fixed number of threads to work with, along with a small queue in front of it.

When the thread pool is at capacity, operations will begin getting rejected from the pool, resulting in an immediately-thrown Exception.

*Configuration + Defaults*

```
# Number of threads to allocate.
mjolnir.pools.<pool-key>.threadCount=10

# Length of the queue that fronts the pool.
mjolnir.pools.<pool-key>.queueLength=10
```

Changing these values requires an application restart (i.e. pools don't dynamically resize after creation).

Circuit Breakers
-----

Breakers track the success/failure rates of operations, and trip if the failure rate exceeds a configured threshold. A tripped breaker immediately rejects operations that attempt to go through it.

After a configured period, the breaker sends a test operation through. If the operation succeeds, the breaker fixes itself and allows operations again.

*Configuration + Defaults*

```
# The minimum operation count the breaker must see before considering tripping.
mjolnir.breaker.<breaker-key>.minimumOperations=10

# The error percentage at which the breaker should trip.
mjolnir.breaker.<breaker-key>.thresholdPercentage=50

# When the breaker trips, the duration to wait before allowing a test operation.
mjolnir.breaker.<breaker-key>.trippedDurationMillis=10000

# Forces the breaker tripped. Takes precedence over forceFixed.
mjolnir.breaker.<breaker-key>.forceTripped=false

# Forces the breaker fixed.
mjolnir.breaker.<breaker-key>.forceFixed=false

# Period to accumulate metrics in. Resets at the end of every window.
mjolnir.metrics.<breaker-key>.windowMillis=30000

# How long to cache the metrics snapshot that breakers read. Probably doesn't need adjusting.
mjolnir.metrics.<breaker-key>.snapshotTtlMillis=1000
```

These values can be changed at runtime.

Fallbacks
-----

TODO (+ semaphores)

Timeouts / Cancellation
-----

Every command supports cooperative cancellation using a [CancellationToken](http://msdn.microsoft.com/en-us/library/system.threading.cancellationtoken(v=vs.110).aspx).

TODO
- What happens when the timeout is reached?
- Timeouts can be configured, and should be tuned after observing metrics.
