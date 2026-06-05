# Starting a mod from zero

The shape of a Vintage Story mod, so a generated `.dll` (or asset pack) loads cleanly. For the API
itself see `api-usage.md`; for patching see `harmony-usage.md`.

## Two kinds of mod (you can combine them)

- **Content mod** — JSON + assets only (block/item types, recipes, lang, textures, shapes). No code,
  no compile. Ships as a folder or `.zip` in the game's `Mods/`.
- **Code mod** — C# compiled to a single `.dll` (a `ModSystem` plus optional `Block`/`Item`/
  `BlockEntity`/behavior classes). Ships as that `.dll` in `Mods/`. Code mods can carry assets too.

## Required metadata

Every mod declares its identity. For a code mod, embed it in the assembly:

```csharp
[assembly: ModInfo("My Mod", "mymod",
    Description = "What it does",
    Side = "Universal",                 // "Server", "Client", or "Universal"
    Version = "1.0.0",
    Authors = new[] { "you" })]
```

A content mod uses a `modinfo.json` at its root with the same fields (`type: "content"` /
`"code"`, `modid`, `name`, `version`, `dependencies`, …).

## Code-mod project (.csproj)

A plain `net*` library that references the game assemblies from a local VS install (mark them
`Private=false` so they are **not** copied into your output — the game already has them):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>     <!-- match the VS runtime -->
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <VintagestoryDir>$(VINTAGE_STORY)</VintagestoryDir> <!-- or %APPDATA%\Vintagestory -->
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="VintagestoryAPI"><HintPath>$(VintagestoryDir)\VintagestoryAPI.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="VintagestoryLib"><HintPath>$(VintagestoryDir)\VintagestoryLib.dll</HintPath><Private>false</Private></Reference>
    <!-- only if you Harmony-patch: -->
    <Reference Include="0Harmony"><HintPath>$(VintagestoryDir)\Lib\0Harmony.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```

The build output is one `.dll` (your mod). Mods reference the **public API** (`VintagestoryAPI.dll`);
reach into `VintagestoryLib.dll` only for engine internals (and prefer the API or Harmony — see
`harmony-usage.md`).

## Assets layout (for content, or a code mod that ships content)

```
assets/<modid>/
├── blocktypes/   <name>.json   (a code-mod block sets "class" + "entityClass" to your registered names)
├── itemtypes/    <name>.json
├── recipes/      grid/, smithing/, …
├── lang/         en.json       (display names: "block-<modid>:<name>": "…")
├── textures/     block/, item/ (PNG)
└── shapes/       block/, item/ (JSON shapes)
```

## The loop

1. Build the code mod (`dotnet build -c Release`) → one `.dll`.
2. Drop the `.dll` (and/or the asset folder/zip) into the server or client `Mods/` directory.
3. Restart (server) or relaunch (client). Watch the log for `[modid]` lines and load errors.

For server iteration, scripting the copy + restart speeds the loop up. Keep all mod artifacts in their
own version control; the game binaries stay where the game installed them.
