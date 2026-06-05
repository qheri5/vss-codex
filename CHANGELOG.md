# Changelog

All notable changes to **vss-codex** (the tool). Follows [Keep a Changelog](https://keepachangelog.com)
and [Semantic Versioning](https://semver.org). This is distinct from the per-game-version
`CHANGELOG-<old>-to-<new>.md` files the tool generates.

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
