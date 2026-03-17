# RYZECHØ Prototype

`RYZECHØ` is a playable C# prototype for the tactical-economy-hiding FPS pitch. Instead of attempting full 3D from an empty repo, this version focuses on the core loop in a top-down combat slice:

- `CONSTRUCT`: spend AP one time to place blast doors, honey traps, and static nests.
- `BET`: choose the round boss, set the stake, and buy your weapon before deploying.
- `HUNT`: defend the data core with a 120-degree vision cone while enemy footsteps and gunfire appear as audio ripple cues.

## Tech

- `.NET 10`
- `Windows Forms`
- Pure C# and GDI+ rendering

## Run

```powershell
dotnet run --project .\RYZECHo.Prototype\RYZECHo.Prototype.csproj
```

## Controls

### Construct

- `1 / 2 / 3`: select blast door, honey trap, or static nest
- `Tab`: cycle build tools
- `Left Click`: place selected structure on a highlighted slot
- `Right Click`: refund the structure on that slot
- `Enter`: lock the fortification and move to betting

### Bet

- `1 / 2 / 3`: choose the boss for the round
- `Q / E`: cycle your weapon
- `A / D`: decrease or increase the bet
- `Enter`: commit the loadout and start the round

### Hunt

- `W / A / S / D`: move
- `Mouse`: aim
- `Hold Left Click`: fire

### Global

- `Space`: show or hide the briefing overlay
- `R`: restart after victory or defeat

## Prototype Notes

- The prototype keeps the requested economy loop: stake money before a round, then only receive the doubled payout if the chosen boss survives a win.
- Weapon choice affects visibility and hearing. The sniper sees farther, while the SMG is better at reading ripple cues.
- Honey traps slow raiders and amplify their footsteps. Static nests create noisy fake ripple traffic and reduce visibility inside their radius.
- Blast doors are breakable, so fortifying a lane buys time instead of creating a permanent lockout.
