# Changelog

All notable changes to **vss-codex** (the tool). Follows [Keep a Changelog](https://keepachangelog.com)
and [Semantic Versioning](https://semver.org). This is distinct from the per-game-version
`CHANGELOG-<old>-to-<new>.md` files the tool generates.

## [1.3.0] - 2026-06-05

### Added
- **"View decompiled source" links**: each API type's page now links to its decompiled `.cs` (the
  outermost declaring type's file), so you can jump straight from the doc to the real implementation.
  Lightweight (a plain link to the `.cs`, not a rendered copy — the search index stays lean) and emitted
  only when the file exists, so a partial or skipped decompile simply yields no link. The `serve-docs`
  launchers now serve the reference root and open `/site/index.html`, so the source links into
  `decompiled/` resolve over HTTP (like search, this needs the local server, not a `file://` page).

## [1.2.2] - 2026-06-05

### Changed
- Quieter, cleaner console: drop the redundant `steps : N` line from the startup banner, and capture
  `mkdocs build` output — show it only if the build fails. On success this hides MkDocs' INFO chatter
  and the upstream Material-for-MkDocs "MkDocs 2.0" advocacy banner (an informational notice from the
  theme authors, not an error; the tool pins a known-good `mkdocs-material` and is unaffected).
- README: make the `serve-docs.cmd` / `serve-docs.sh` one-click browse step prominent (don't open
  `index.html` directly — the search needs the local server).

## [1.2.1] - 2026-06-05

### Fixed
- When the prebuilt `.exe` is launched by double-clicking it on Windows, wait for Enter before exiting
  so the console output (and any error) stays readable instead of the window vanishing. No-op when run
  from a shell, when output is piped, or on other platforms.

### Added
- The site step now writes `serve-docs.cmd` / `serve-docs.sh` into `out/reference/site/`: double-click
  to serve the docs and open them at `http://localhost:8000`. This is how the Material **search** works
  — it loads its index over HTTP and can't run from a `file://` page (opening `index.html` directly
  shows the pages but no working search).

## [1.2.0] - 2026-06-05

### Fixed
- Cap the one-off `dotnet tool install` for `ilspycmd` with a timeout so a stalled NuGet restore can no
  longer hang the whole run.
- Render a sealed override as `sealed override` (was `override sealed`).
- Don't borrow a differently-shaped base overload's XML summary for an inherited method that has no
  matching base overload (arity is now checked).
- Make the game-version fallback null-safe and warn when `GameVersion.ShortGameVersion` is absent.

### Security
- Harden archive extraction (`--zip`): extract entry-by-entry, reject any entry whose path escapes the
  destination (zip-slip / tar-slip), and skip symlink/hardlink entries. Extract into a unique per-run
  temp dir so concurrent/stale runs can't collide.

### Changed
- Pin `mkdocs-material` to a known-good version and key the cached venv's readiness sentinel on it, so a
  breaking theme release can't silently break the site and a version bump re-installs.
- Internal: share the public-API surface query, member-visibility predicates, the doc-generator base,
  and the timed subprocess runner across the generators / snapshot / steps (removes duplication so the
  docs and the changelog snapshot can't drift). Memoize inherited-doc ancestor lookups. Extract a
  unit-testable `Options.Parse`; warn when both `--zip` and `--install` are given. Remove dead code.

### Tests / CI
- Add tests for the archive extractor (incl. zip-slip), type/modifier rendering, option parsing, the
  generators, inherited-doc overload disambiguation, and XML-doc tag flattening. Run CI on macOS too.

## [1.1.0] - 2026-06-05

### Added
- Browsable site — the knowledge base is now also rendered as an ordered, searchable static site with
  [MkDocs](https://www.mkdocs.org) + the Material theme, written to `out/reference/site/`. It is built
  by the real MkDocs (the tool only generates `mkdocs.yml` and runs `mkdocs build` as a subprocess —
  nothing is reimplemented). Browse it offline by serving the folder, e.g. `python -m http.server`.
- MkDocs + `mkdocs-material` are auto-installed into a cached per-user virtualenv on first run. The
  step is **optional and non-fatal**: if Python 3 is unavailable it is skipped with a clear note, and
  the markdown knowledge base and skill (the core output) are produced regardless.
- `--no-site` — skip building the site (the markdown is still produced).

### Notes
- The site is derived from the same proprietary-binary output, so it also lives under the gitignored
  `out/` tree — keep it local, do not redistribute.

## [1.0.0] - 2026-06-05

First public release.

### Added
- Cross-platform .NET tool — one command (`dotnet run --project src/VssCodex`) decompiles the Vintage
  Story binaries (via `ilspycmd`) and generates a markdown knowledge base from their metadata
  (Mono.Cecil): the API reference, an events/enums index, the engine-internal type surface, the
  Harmony-patchable catalog, and a version-diff CHANGELOG — plus a rendered Claude Code skill. All
  output lands in a gitignored `out/` folder.
- Converter mode (`--zip`) — point it at a downloaded VS server/client `.zip` or `.tar.gz`; it extracts
  and locates the binaries automatically.
- Runs identically on Windows, Linux, and macOS. Default install resolves via `VINTAGE_STORY` or, on
  Windows, `%APPDATA%\Vintagestory`.
- Self-contained single-file binaries for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64`.
- xUnit test suite covering the version-sensitive generator logic (doc-ids, signatures, XML-summary
  flattening, inherited docs, Harmony patchability, snapshot/changelog diff).

### Notes
- The generated output is derived from proprietary Vintage Story binaries — it is never committed or
  redistributed. A legally obtained copy of the game is required to run the tool.
