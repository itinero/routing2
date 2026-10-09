# Contributing to Itinero

## Formatting

`.editorconfig` is the authority. Run `dotnet format` before pushing.

## Comments

Two lines maximum, counting prose only — a `<summary>` whose text spans two lines is fine, its
tags are not counted.

Comment why, not what:

```csharp
// No U-turn: the search is edge-based, so leaving on the arriving edge is a reversal.
if (neighbourEdge == edge) continue;
```

Longer reasoning goes in the commit message. So do measurements: "cut FindNoLock 1.34B to 112M
calls" is true of a commit, not of the code.

XML docs state the contract: what to pass, what comes back, what makes it throw or return null.
Drop `<remarks>` unless it changes how a caller uses the member.

## Naming

Name what a thing is, not how it came about. `RouteCorridorSearch`, not
`MergedCorridorSearch` — rename once the thing it was contrasted against is gone.

## Tests

Name the property being claimed: `BudgetBelowTheDetour_ExcludesIt`, not `TestCorridor2`.

Prefer a hand-built network with one known answer over a real-world extract. Say so when a fixture
is deliberately extreme, or it will be tuned and the test's purpose lost.

`AddEdge` across a tile boundary returns no usable `EdgeId`. Keep both endpoints in one tile and
use a `shape` to make an edge long.

## Commits

One change per commit, no prefix tags:

```
Replace the global edge id dictionary with a striped open-addressed table.
Grow tile buffers geometrically and trim them when the write is done.
```

Measurements and reasoning go in the body.

## Before a pull request

- `dotnet format`
- `dotnet test test/Itinero.Tests/Itinero.Tests.csproj`
- Check the longest comment you added is two lines or fewer.
