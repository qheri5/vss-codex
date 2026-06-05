# Changelog

All notable changes to **vss-codex** (the tool). Follows [Keep a Changelog](https://keepachangelog.com)
and [Semantic Versioning](https://semver.org). This is distinct from the per-game-version
`CHANGELOG-<old>-to-<new>.md` files the tool generates.

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
