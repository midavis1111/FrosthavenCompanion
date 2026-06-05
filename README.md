# Frosthaven Companion

A personal companion app for the [Frosthaven](https://en.wikipedia.org/wiki/Frosthaven) board game.
First feature: **campaign scenario-progress tracking**.

Built with **.NET 10 / Blazor WebAssembly** and hosted for free on **GitHub Pages** — it runs
entirely in your browser with no server or cloud cost. Your campaign is saved in the browser's
local storage, with one-click JSON **export/import** for backups and moving between devices.

## Projects

| Project | What it is |
| --- | --- |
| `src/FrosthavenCompanion.Domain` | Pure C# campaign model and rules (no UI dependency). |
| `src/FrosthavenCompanion.App` | Blazor WebAssembly PWA — the UI shell over the domain. |
| `tests/FrosthavenCompanion.Domain.Tests` | xUnit tests for the domain logic. |

> The solution file is `FrosthavenCompanion.slnx` (the .NET 10 XML format). Pass it explicitly to
> `dotnet build` / `dotnet test`.

## Run locally

```bash
dotnet run --project src/FrosthavenCompanion.App
```

Then open the URL it prints (e.g. `http://localhost:5095`).

## Test

```bash
dotnet test FrosthavenCompanion.slnx
```

## Deploy

Pushing to `main` triggers `.github/workflows/deploy.yml`, which tests, publishes the WASM app,
and deploys it to GitHub Pages. The site is served from the project subpath, so the workflow
rewrites the `<base href>` to `/FrosthavenCompanion/`, adds `.nojekyll`, and creates a `404.html`
SPA fallback.

**One-time setup:** in the GitHub repo, go to **Settings → Pages → Build and deployment** and set
**Source** to **GitHub Actions**. After the first successful run the app is live at
<https://midavis1111.github.io/FrosthavenCompanion/>.
