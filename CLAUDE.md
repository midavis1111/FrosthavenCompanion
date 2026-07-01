# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A **personal, single-user** companion app for the Frosthaven board game. It's a .NET 10 **Blazor WebAssembly (standalone) PWA**, hosted free on GitHub Pages, with browser-local storage and optional cross-device sync. There is no server/backend — everything runs in the browser.

Features so far: campaign **scenario-progress tracking** that mirrors the game's spoiler-free flow chart (list view with filter/search + a pan/zoom visual flow chart), per-scenario **rewards / descriptions / complexity / enemies / unlock-source**, **manual unlocks**, a **party page** (`/party`: characters with class + level → derives the recommended scenario level and its gold/trap/hazardous/XP chart), a **campaign status panel** (`/status`: week, prosperity, morale, inspiration, defense, reputation, and the town resource stockpile), an **outpost page** (`/outpost`: the outpost-phase checklist + a building tracker with official building-card art), **cross-device sync** via a private gist, a **monster stat reference** (`/monsters`) with official stat-card art, a per-scenario **setup view** (`/scenario/{index}`: pick a level, see every enemy's stat card), and a **conditions glossary** (`/conditions`) with official condition icons. Per-scenario **loss/retry counts** are tracked on the tracker cards. Not built yet (see project memory `backlog`): per-scenario notes, per-character gold/XP/items/perks/personal-quests (the party page tracks only class + level so far), monster ability-card decks.

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

`Services/CampaignStore.cs` is the single seam between UI and domain/persistence. Razor pages never touch localStorage, the engine, or the gist API directly — they call the store, which holds `Progress`, persists to localStorage, mirrors to the gist when connected, and exposes `Views` (the derived scenario list). `Pages/Home.razor` is the list tracker (with filter/search); `Pages/Party.razor` (`/party`) is the character roster + derived scenario level; `Pages/MyCharacter.razor` (`/character`) is the player's own single-character sheet (level/XP/gold/resources/notes/perks/masteries; class/level shared with Party); `Pages/FlowChart.razor` is the visual graph (an SVG positioned by each scenario's `X`/`Y` flow-chart coordinates, showing only revealed nodes, with unlock arrows, click-to-complete, and pan/zoom); `Pages/Party.razor` (`/party`) is the character roster + derived scenario level (`ScenarioLevelTable` does the rules); `Pages/StatusPage.razor` (`/status`) is the campaign status panel; `Pages/Outpost.razor` (`/outpost`) is the outpost-phase checklist + building tracker; `Pages/MonsterReference.razor` (`/monsters`) is the monster stat reference; `Pages/ScenarioSetup.razor` (`/scenario/{Index}`) is a per-scenario setup view (pick a level, see every enemy's stat card at that level — spoiler-gated: shows nothing for `Hidden` scenarios); `Pages/ConditionsPage.razor` (`/conditions`) is the conditions glossary; `Pages/Sync.razor` is sync setup. The shared `Components/MonsterStatCard.razor` renders one monster's official stat-card art + the exact-level data table (full mode on `/monsters`, `Compact` mode in the scenario grid), resolving the card image via `Services/MonsterCardService.cs`. `CampaignStore.LoadAsync` is idempotent (loads/reconciles once per session) so navigating between pages doesn't re-sync.

Monster stats and conditions are read-only reference data, separate from the campaign: `MonsterCatalog` (embedded `Data/monsters.json`, singleton) and the authored static `Conditions` class. Scenario enemy lists link to `/monsters?m=<name>`, and condition names show tooltips from `Conditions.Find(...)`. See the project memory `monsters-conditions` for the data shape and the **stringified-stats** regen gotcha (boss stats can be formulas like "Cx20").

### Persistence and sync

- Local: `localStorage` key `frosthaven.progress` (the serialized `CampaignProgress`). JSON export/import in the UI is the same payload.
- Cross-device sync (`Services/GistSyncService.cs`): mirrors the save to a **private GitHub gist**, calling the GitHub REST API **directly from the browser** (CORS works for api.github.com; do not set a `User-Agent` header — browsers forbid it). Auth is a classic PAT with `gist` scope, stored in localStorage (`frosthaven.sync.token`, `frosthaven.sync.gistId`). Reconciliation is **newest-wins** via `CampaignProgress.UpdatedAt`: on load, pull remote and adopt it if newer, else push local; every change stamps `UpdatedAt` and pushes.

## Reference data (embedded)

All under `src/FrosthavenCompanion.Domain/Data/`, each an `EmbeddedResource`. The scenario unlock graph + monster stats are *facts* derived from community datasets (not their code); descriptions and condition text are authored; the condition icon PNGs are official Cephalofair art used under their non-commercial fan policy (see Deployment for the required disclaimer).

- **`scenarios.json`** — the scenario catalog (see below).
- **`descriptions.json`** — authored short scenario recaps (index → text), kept **separate from scenarios.json so regenerating the catalog never wipes them**; `ScenarioCatalog.LoadEmbedded` merges them in. 141/141 written.
- **`monsters.json`** — 122 monster stat blocks (`MonsterCatalog`, singleton). Regenerate via sparse-checkout of `Lurkars/gloomhavensecretariat` `data/fh/monster/*.json` (+ `data/fh/label`), excluding `*-solo.json`, merging each entry over `baseStat`. **Stat values are stored as strings** (health/move/attack/range) because boss/summon stats can be formulas like "Cx20" — System.Text.Json won't coerce mixed number/string. Range is usually absent (it lives on ability cards).
- **`wwwroot/icons/conditions/{slug}.png`** (in the App, not Domain) — official condition icons from `any2cards/worldhaven` `images/art/frosthaven/icons/conditions/fh-{name}-color-icon.png`. The static `Conditions` class (15 standard conditions, authored descriptions) maps each to its icon slug. Referenced conditions render icon-only with a tooltip; the `/conditions` page shows icon + name + text.
- **`wwwroot/icons/buildings/{num}-{slug}-{level}[-back].png`** (in the App, not Domain) — official outpost building-card art from `any2cards/worldhaven` `images/outpost-building-cards/frosthaven/fh-{num}-{slug}-level-{L}[-back].png` (front = operational, `-back` = damaged/wrecked). The static `BuildingCatalog` (22 buildable buildings: number, slug, name, max level — authored, costs/effects read off the art) drives `/outpost`. `CampaignProgress.Buildings` (slug → `{Level, Condition}`) and `OutpostChecklist` (ticked step keys) are the stored state; `OutpostPhase.Steps` is the authored checklist.
- **`wwwroot/icons/monster-cards/{slug}-{band}.png`** + **`manifest.json`** (in the App, not Domain) — official monster **stat-card** art from `any2cards/worldhaven` `images/monster-stat-cards/frosthaven/fh-{slug}-{band}.png`. Each card covers a **level band** (normal monsters: `0` = levels 0–3, `4` = 4–7; bosses: `0/2/4/6`); **66 of 122** monsters have cards. `manifest.json` (generated at download time) maps `slug → [bands]`; `MonsterCardService` loads it once and `CardImage(slug, level)` returns `{slug}-{highest band ≤ level}.png` (or null → the table-only fallback). The art shows *all four* levels of its band around the edges, so the data table beside it gives the precise selected-level numbers.
- **`wwwroot/icons/monsters/{slug}.png`** (in the App, not Domain) — official monster portrait art from `any2cards/worldhaven` `images/art/frosthaven/monsters/images/fh-{slug}.png`, saved as `{slug}.png` (the monster's `name`). **64 of 122** monsters have one (the standard/recurring standees); the rest are **bosses & named one-offs** whose art is filed elsewhere with different naming — not downloaded, so `/monsters` hides the `<img>` via `onerror`. Scenario/section variants (`-scenario-N`/`-section-N` suffix) reuse the base slug's portrait via `PortraitSlug(...)`. Fetched in-browser only when a monster is viewed (not in the WASM bundle). Same Cephalofair fan-content terms as the condition icons.
- **`cards.json` + `perks.json`** (Domain, `EmbeddedResource`) + **`wwwroot/icons/cards/{classSlug}/{cardSlug}.png`** (App) — per-class ability cards (`CardCatalog`: `{handSize, cards:[{id,name,level,initiative,image}]}`) and perks/masteries (`PerkCatalog`: `{perks:[{text,boxes}], masteries:[]}`). **Authored classes: Blinkblade, Deathwalker, Geminate, Trapper, Pyroclast.** Card art from `any2cards/worldhaven` `images/character-ability-cards/frosthaven/{abbrev}/` (bb/dw/ge/ta/py). Generated from `Lurkars/gloomhavensecretariat` `data/fh/character/{slug}.json` (perks) + `deck/{slug}.json` (cards) + `label/en.json` (`custom.fh.{slug}.N` text). GHS uses spoiler-free codenames for locked classes (**Pyroclast = `meteor`**, Trapper = `trap`); **neither has `custom.fh` labels upstream** → their custom perks/masteries are placeholders needing verification. The `/classes` page is a read-only gallery of all this. See project memory `characters-perks`.

## Scenario catalog data

`src/FrosthavenCompanion.Domain/Data/scenarios.json` is an `EmbeddedResource` (logical name `FrosthavenCompanion.Domain.Data.scenarios.json`), derived from the community `Lurkars/gloomhavensecretariat` dataset (`data/fh/scenarios/*.json`) — only the factual unlock graph, not their code. To regenerate: sparse-checkout that repo's `data/fh/scenarios` **and** `data/fh/label`, then for each campaign scenario (filename `^[0-9]+[A-Z]?\.json$`, excluding `solo*`/`random`) extract index/name/initial/complexity/monsters/unlocks/blocks/requires/links/coords, and render the `rewards` object into human-readable strings (resolving `%data.…%`/`%game.…%` placeholders against `data/fh/label/en.json`). Their `requires` is a list-of-groups (AND/OR) that we **flatten** to a plain required-list (only scenarios 17/31/52 use it).

The generator applies manual **name overrides** (currently `22` → "Ice Flows") — keep that map when regenerating or the override reverts. The catalog's unlock graph is **partial** (~53 of 141 scenarios have edges; the rest are unlocked via the section book, which is not in any structured dataset) — this is by design, and `ManualUnlocks` is the safety valve. Because the full graph + reward text ship in the static bundle, they're technically readable via dev-tools; spoiler protection is at the UI layer (Hidden scenarios aren't shown; rewards show only on completed scenarios).

## Deployment

Pushing to `main` triggers `.github/workflows/deploy.yml`: test → publish the WASM app → rewrite `<base href="/">` to `/FrosthavenCompanion/` (required for the GitHub Pages project subpath) → add `.nojekyll` (so `_framework` isn't ignored) → add `404.html` SPA fallback → deploy. Live at https://midavis1111.github.io/FrosthavenCompanion/. The Pages "Source" repo setting is "GitHub Actions" (one-time, already done).

Note: the `gh` CLI token in the dev environment is a **different account** (`midavisbiz`) that is **not** the repo owner — it has no push access (push via gh token → 403) and can't re-run/dispatch workflows. Working pushes use the Windows **Git Credential Manager** with the owner's creds, but in headless/agent sessions GCM often **hangs** (the `git push` silently backgrounds / times out without advancing the remote ref). So the agent frequently **cannot push** — commit locally and ask the **user to run `git push` from their own terminal** (that triggers the deploy). Some pushes work when GCM is "warm" from a recent manual push.

Condition icons are official Cephalofair art used under their non-commercial fan-content policy; the required trademark/copyright disclaimer is in `Layout/MainLayout.razor` (app-wide footer). Keep it. The deploy workflow still uses Node20 GitHub Actions (deprecation warning only).
