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

```
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

```
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

```
[Command("core-client", "core-team", 15000)]
public interface ITeamService
{
    TeamDto GetTeam(string teamId, CancellationToken? token = null);
    void UpdateTeam(TeamDto teamDto, CancellationToken? token = null);
}

public class TeamService : ITeamService { /* ... */ }

public void Main() {
    // This is typically done in a service locator of some sort and cached.
    var proxy = CommandInterceptor.CreateProxy<ITeamService>(new TeamService());
    var teamDto = proxy.GetTeam("1234");
}
```

Using `[Command]` provides the same benefits as extending `Command<TResult>`, but can be more convenient at times.

Note the presence of `CancellationToken`s on the interface methods. If your interface method signature contains a `CancellationToken` (which is optional), Mjolnir will pass a token created from the timeout through to your method, allowing you to cooperatively cancel your operation. If no `CancellationToken` parameter is present, the Command timeout may be less effective. Mjolnir will only pass its token through if it doesn't already see a non-null or non-empty token as the parameter value.

Thread Pools
-----

TODO

Circuit Breakers
-----

TODO

Fallbacks
-----

TODO (+ semaphores)

Timeouts
-----

TODO
- What happens when the timeout is reached?
- Timeouts can be configured, and should be tuned after observing metrics.
