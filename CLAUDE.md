# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A **personal, single-user** companion app for the Frosthaven board game. First and current feature: campaign **scenario-progress tracking** that mirrors the game's spoiler-free flow chart. It's a .NET 10 **Blazor WebAssembly (standalone) PWA**, hosted free on GitHub Pages, with browser-local storage and optional cross-device sync. There is no server/backend — everything runs in the browser.

## Commands

The solution file is `FrosthavenCompanion.slnx` (the .NET 10 XML format). **Pass it explicitly** — `dotnet build`/`test` do not auto-discover `.slnx` the way they do `.sln`.

```bash
dotnet build FrosthavenCompanion.slnx              # build everything
dotnet test FrosthavenCompanion.slnx              # run all tests
dotnet run --project src/FrosthavenCompanion.App  # run the app (http://localhost:5095)

# Run a single test by name (or any xUnit filter expression):
dotnet test tests/FrosthavenCompanion.Domain.Tests --filter "FullyQualifiedName~Choosing_one_branch_locks_out_the_other"
```

Stop any running `dotnet run` before building/testing — it locks the App assembly and the build will fail.

## Architecture

Three projects: `src/FrosthavenCompanion.Domain` (pure C# rules, no UI dependency), `src/FrosthavenCompanion.App` (Blazor WASM UI), `tests/FrosthavenCompanion.Domain.Tests` (xUnit). Keep all game logic in Domain so the UI/hosting can change without touching the rules.

### The core design: derive everything from what's completed

This is the key idea to understand before changing scenario behavior. State is **not** stored per-scenario. Instead:

- **`CampaignProgress`** is the *only* persisted state — party name, the set of **completed** scenario indices (with dates), **manual unlocks**, notes, and an `UpdatedAt` timestamp. It's deliberately tiny.
- **`ScenarioCatalog`** is read-only reference data: all 141 campaign scenarios with their unlock graph (`Unlocks`/`Blocks`/`Requires`/`Links`), loaded from the embedded `Data/scenarios.json`.
- **`CampaignEngine`** *derives* every scenario's `ScenarioStatus` (Hidden/Available/Locked/LockedOut/Completed) from catalog + the completed set, recomputed on every render via `BuildViews`. Nothing about availability is stored.

This makes **undo trivial** (just remove from the completed set and re-derive) and prevents state drift. When adding scenario behavior, prefer extending the derivation in `CampaignEngine.Status(...)` over adding stored flags.

The reveal rules mirror real Frosthaven (`CampaignEngine` is the single source of truth):
- A scenario is revealed by being `Initial`, or by being in the `Unlocks`/`Links` of a **completed** scenario (reveal-on-completion).
- Completing a scenario locks out everything in its `Blocks` (the branch not taken).
- A revealed scenario is `Available` once its `Requires` are all completed, else `Locked`.
- **Manual unlocks** (`CampaignProgress.ManualUnlocks`) cover unlocks from outside the scenario graph — class personal quests, town/outpost events, items, section book. They force a scenario to `Available`.
- Status precedence: `Completed` > manual unlock > `LockedOut` > `Hidden` > `Locked`/`Available`.

### UI talks only to CampaignStore

`Services/CampaignStore.cs` is the single seam between UI and domain/persistence. Razor pages never touch localStorage, the engine, or the gist API directly — they call the store, which holds `Progress`, persists to localStorage, mirrors to the gist when connected, and exposes `Views` (the derived scenario list). `Pages/Home.razor` is the list tracker (with filter/search); `Pages/FlowChart.razor` is the visual graph (an SVG positioned by each scenario's `X`/`Y` flow-chart coordinates, showing only revealed nodes, with unlock arrows, click-to-complete, and pan/zoom); `Pages/MonsterReference.razor` (`/monsters`) is the monster stat reference; `Pages/ConditionsPage.razor` (`/conditions`) is the conditions glossary; `Pages/Sync.razor` is sync setup. `CampaignStore.LoadAsync` is idempotent (loads/reconciles once per session) so navigating between pages doesn't re-sync.

Monster stats and conditions are read-only reference data, separate from the campaign: `MonsterCatalog` (embedded `Data/monsters.json`, singleton) and the authored static `Conditions` class. Scenario enemy lists link to `/monsters?m=<name>`, and condition names show tooltips from `Conditions.Find(...)`. See the project memory `monsters-conditions` for the data shape and the **stringified-stats** regen gotcha (boss stats can be formulas like "Cx20").

### Persistence and sync

- Local: `localStorage` key `frosthaven.progress` (the serialized `CampaignProgress`). JSON export/import in the UI is the same payload.
- Cross-device sync (`Services/GistSyncService.cs`): mirrors the save to a **private GitHub gist**, calling the GitHub REST API **directly from the browser** (CORS works for api.github.com; do not set a `User-Agent` header — browsers forbid it). Auth is a classic PAT with `gist` scope, stored in localStorage (`frosthaven.sync.token`, `frosthaven.sync.gistId`). Reconciliation is **newest-wins** via `CampaignProgress.UpdatedAt`: on load, pull remote and adopt it if newer, else push local; every change stamps `UpdatedAt` and pushes.

## Scenario catalog data

`src/FrosthavenCompanion.Domain/Data/scenarios.json` is an `EmbeddedResource` (logical name `FrosthavenCompanion.Domain.Data.scenarios.json`), derived from the community `Lurkars/gloomhavensecretariat` dataset (`data/fh/scenarios/*.json`) — only the factual unlock graph, not their code. To regenerate: sparse-checkout that repo's `data/fh/scenarios` **and** `data/fh/label`, then for each campaign scenario (filename `^[0-9]+[A-Z]?\.json$`, excluding `solo*`/`random`) extract index/name/initial/complexity/monsters/unlocks/blocks/requires/links/coords, and render the `rewards` object into human-readable strings (resolving `%data.…%`/`%game.…%` placeholders against `data/fh/label/en.json`). Their `requires` is a list-of-groups (AND/OR) that we **flatten** to a plain required-list (only scenarios 17/31/52 use it).

The generator applies manual **name overrides** (currently `22` → "Ice Flows") — keep that map when regenerating or the override reverts. The catalog's unlock graph is **partial** (~53 of 141 scenarios have edges; the rest are unlocked via the section book, which is not in any structured dataset) — this is by design, and `ManualUnlocks` is the safety valve. Because the full graph + reward text ship in the static bundle, they're technically readable via dev-tools; spoiler protection is at the UI layer (Hidden scenarios aren't shown; rewards show only on completed scenarios).

## Deployment

Pushing to `main` triggers `.github/workflows/deploy.yml`: test → publish the WASM app → rewrite `<base href="/">` to `/FrosthavenCompanion/` (required for the GitHub Pages project subpath) → add `.nojekyll` (so `_framework` isn't ignored) → add `404.html` SPA fallback → deploy. Live at https://midavis1111.github.io/FrosthavenCompanion/. The Pages "Source" repo setting is "GitHub Actions" (one-time, already done).

Note: the `gh` CLI token in the dev environment lacks admin rights to re-run or `workflow run` dispatch. To trigger a deploy, **push** (an empty commit works) rather than re-running via `gh`.
