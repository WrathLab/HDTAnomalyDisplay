# HDT Anomaly Display — patched

A Hearthstone Deck Tracker plugin that displays the current Battlegrounds anomaly on the overlay.

**This is a fork of [Mouchoir/HDTAnomalyDisplay](https://github.com/Mouchoir/HDTAnomalyDisplay) with the v1.3.0 patch.** The original plugin has been broken on modern HDT since late 2023; this fork fixes it.

## The bug

`AnomalyDisplay.AwaitGameEntity` waits up to 10 seconds for 2 hero entities with the `BACON_HERO_CAN_BE_DRAFTED` or `BACON_SKIN` tags. Those tags only appear during the **initial BG hero select** (one-time per BG run). On every normal match the loop times out, and the anomaly read that follows never runs.

The whole flow also has no try/catch, so a single null entity in the game state silently kills the plugin with no log to debug.

## The fix

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

## Install

Copy `bin\Release\HDTAnomalyDisplay.dll` to your HDT plugins folder:

```
%AppData%\HearthstoneDeckTracker\Plugins\HDTAnomalyDisplay.dll
```

(typically `C:\Users\<you>\AppData\Roaming\HearthstoneDeckTracker\Plugins\`)

Restart HDT. Open the plugin's settings menu, click **Unlock Overlay** to drag the anomaly card to your preferred position, **Reset Position** to put it back at the default (X=0, Y=630).

## What you should see in HDT's log

On a successful run:

```
[HDTAnomalyDisplay] OnLoad
[HDTAnomalyDisplay] Game entity exposed anomaly DbfId=... on attempt N
[HDTAnomalyDisplay] Anomaly DbfId found: <id>
```

Common failure modes (logged as warnings, not crashes):

- `HandleGameStart: not a Battlegrounds game` — you opened HDT while in main menu or constructed
- `GameEntity still null after wait` — HDT didn't pick up the game state
- `No anomaly DbfId found` — this BG match genuinely has no anomaly (most don't)
- `HandleGameStart failed: <exception>` — something is wrong, the exception type and stack are logged

## Credits

Original plugin by [@Mouchoir](https://github.com/Mouchoir) and [@Tignus](https://github.com/Tignus) — all credit to them for the design. v1.3.0 patch is a maintenance fix; no feature work.
