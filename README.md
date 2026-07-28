# HDT Anomaly Display (maintained fork)

A Hearthstone Deck Tracker plugin that displays the current Battlegrounds anomaly on the overlay.

> **Status: maintained fork.** The original upstream [Mouchoir/HDTAnomalyDisplay](https://github.com/Mouchoir/HDTAnomalyDisplay) is **archived** as of late 2025 (last push Nov 2023, no responses to issues). This fork is the active version. If you tried the original and it didn't show the anomaly, that's why - and this fork fixes it.

## Quick install

1. Grab the latest `HDTAnomalyDisplay.dll` from [Releases](../../releases) (or build it yourself, see below).
2. Drop it into your HDT plugins folder: `%AppData%\HearthstoneDeckTracker\Plugins\`
3. Restart HDT.
4. Open the plugin's settings (Plugins menu, "HDT Anomaly Display") and click **Unlock Overlay** to drag the card where you want it, or **Reset Position** to put it at the default.

## The bug (in v1.2.0 upstream)

`AnomalyDisplay.AwaitGameEntity` waits up to 10 seconds for 2 hero entities with the `BACON_HERO_CAN_BE_DRAFTED` or `BACON_SKIN` tags. Those tags only appear during the **initial BG hero select** (one time per BG run). On every normal match the loop times out, and the anomaly read that follows never runs.

The whole flow also has no try/catch, so a single null entity in the game state silently kills the plugin with no log to debug.

## The fix (v1.3.0)

- The wait now polls the game entity's anomaly DbfId directly with a 3-second cap, and exits early as soon as the value appears.
- Every public entry point (`HandleGameStart`, `ClearCard`, `OnLoad`, `OnUnload`, settings menu handlers) is wrapped in try/catch.
- Every decision is logged via HDT's `Log.Info` / `Log.Warn` / `Log.Error` so you can see exactly which step succeeded or failed in HDT's log.
- The build is x64 (matching HDT) instead of the original's x86 / AnyCPU mix.

`BattlegroundsUtils.GetBattlegroundsAnomalyDbfId(Entity)` still exists in modern HDT, so that part of the call didn't need changing. The `Core` ambiguity between `Hearthstone_Deck_Tracker.Core` and `Hearthstone_Deck_Tracker.API.Core` is resolved with an explicit `using Core = Hearthstone_Deck_Tracker.API.Core;`.

## Build

You need `HearthstoneDeckTracker.exe` and `HearthDb.dll` from your local HDT install at:

```
C:\Users\<you>\AppData\Local\HearthstoneDeckTracker\app-1.53.15\
```

Drop them into a sibling `lib/` folder:

```
HDTAnomalyDisplay\
  lib\
    HearthstoneDeckTracker.exe
    HearthDb.dll
  HDTAnomalyDisplay.csproj
  ...
```

Then:

```bat
dotnet build -c Release -p:Platform=x64
```

Output: `bin\Release\HDTAnomalyDisplay.dll`

## What you should see in HDT's log

On a successful run:

```
[HDTAnomalyDisplay] OnLoad
[HDTAnomalyDisplay] Game entity exposed anomaly DbfId=... on attempt N
[HDTAnomalyDisplay] Anomaly DbfId found: <id>
```

Common failure modes (logged as warnings, not crashes):

- `HandleGameStart: not a Battlegrounds game` - you opened HDT while in main menu or constructed
- `GameEntity still null after wait` - HDT didn't pick up the game state
- `No anomaly DbfId found` - this BG match genuinely has no anomaly (most don't)
- `HandleGameStart failed: <exception>` - something is wrong, the exception type and stack are logged

## Credits

Original plugin by [@Mouchoir](https://github.com/Mouchoir) and [@Tignus](https://github.com/Tignus) - all credit to them for the design. v1.3.0 patch is a maintenance fix; no feature work.

If Mouchoir ever un-archives the upstream and wants to pull this in, the diff is one self-contained commit against their tree. The PR body is in the conversation log; the maintainer-friendly summary is:

> Fix the 10-second wait for hero entities (those tags only appear during initial BG hero select, not in normal matches). Add try/catch and structured logging around the public entry points. Switch to x64 to match modern HDT. No API surface changes; no behavior changes for users except the plugin now actually works.
