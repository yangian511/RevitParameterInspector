# Installing RevitParameterInspector

There are two ways to register the add-in with Revit. Both need the project built first - see
[docs/build-guide.md](../docs/build-guide.md).

## Option 1: `addin/` - per-user, per-version manifests

`install/addin/` has one ready-to-use `.addin` manifest per supported version
(`RevitParameterInspector2024.addin`, `...2025.addin`, `...2026.addin`, `...2027.addin`). Each one points at
`.\RevitParameterInspector.Revit.dll` - a path relative to the manifest's own location - so:

1. Build the project for your Revit version (see the build guide).
2. Copy that version's `.addin` file **and** the entire build output folder
   (`src/RevitParameterInspector.Revit/bin/<Debug|Release>/<TargetFramework>/` - the `.dll`
   files plus the `dictionary/` and `icons/` folders) into
   `%APPDATA%\Autodesk\Revit\Addins\<version>\`, so the `.addin` file and
   `RevitParameterInspector.Revit.dll` sit side by side.
3. Start Revit. A **ParameterInspector** panel appears on the **Add-Ins** tab with both
   buttons.

This is per-user (Roaming `%APPDATA%`) and per-Revit-version - if you use multiple Revit
versions, repeat this for each one.

For Revit 2027, install the .NET 10 SDK and build against APIs in the default directory
`C:\Program Files\Autodesk\Revit 2027` (or set `RevitInstallDir`):

```powershell
dotnet build src/RevitParameterInspector.Revit -f net10.0-windows -p:RevitVersion=2027 -c Release
```

Copy `install/addin/RevitParameterInspector2027.addin` and the entire
`src/RevitParameterInspector.Revit/bin/Release/net10.0-windows/` output into
`%APPDATA%\Autodesk\Revit\Addins\2027\`. The 2027 manifest retains the 2026 AddInId and
application entry point; the manifests are registered in separate version directories.
Use one installation method per Revit version to avoid duplicate registration.

## Option 2: `bundle/` - a single machine-wide bundle for all versions

`install/bundle/RevitParameterInspector.bundle/` follows Autodesk's multi-version "bundle"
convention (the same layout used by many open-source Revit add-ins): a `PackageContents.xml`
at the bundle root declares which Revit series map to which `.addin` manifest under
`Contents/<version>/`. Revit auto-discovers any `*.bundle` folder placed under
`%ProgramData%\Autodesk\ApplicationPlugins\` for Revit 2024-2026. Revit 2027 discovers
machine-wide bundles under `%ProgramFiles%\Autodesk\ApplicationPlugins\` instead
([Autodesk deployment changes](https://blog.autodesk.io/revit-2027-sdk-net-10-api-changes-and-additions/)).
The package uses one `Components` block with exact `RuntimeRequirements` per version,
so only that version's manifest is selected.

The `.addin` manifests under `Contents/2024|2025|2026|2027/` are already committed; what's missing
until you build is the actual `RevitParameterInspector.Revit.dll` (and its dependencies +
`dictionary/`/`icons/` folders) alongside each one. `build-bundle.ps1` does that for you:

```powershell
# Build and package all four versions (needs all four installed and the .NET 10 SDK)
.\install\bundle\build-bundle.ps1

# Or just the ones you have installed
.\install\bundle\build-bundle.ps1 -Versions 2025,2026 -Configuration Release

# Revit 2027 only
.\install\bundle\build-bundle.ps1 -Versions 2027 -Configuration Release
```

Then copy the whole `RevitParameterInspector.bundle` folder to
`%ProgramData%\Autodesk\ApplicationPlugins\` for 2024-2026 and/or
`%ProgramFiles%\Autodesk\ApplicationPlugins\` for 2027 (administrator privileges required
for the latter), then start Revit. To support old versions and 2027 on one machine, install
the bundle in both locations. Each `Contents/<version>/` must contain that version's own
build output; do not reuse the 2027 DLL for 2025/2026. A partial build packages only the
requested versions; build the others before distributing the full four-version bundle.

The DLLs/`.pdb`/`dictionary/`/`icons/` files the script copies into `Contents/<version>/` are
build output, not source - they're excluded via `.gitignore` (only the `.addin` manifests and
`PackageContents.xml` are tracked).

## Which one should I use?

- Testing a single version quickly, or only ever using one Revit version: `addin/`.
- Distributing to others, or running multiple Revit versions on the same machine: `bundle/`.

Neither is wired into a CI/release pipeline yet - see `docs/roadmap.md`.

## Verified Revit 2027 installation

On 2026-10-06, the maintainer confirmed both per-user manifest loading and machine-wide
bundle loading in actual Revit 2027. The verified bundle directory is
`C:\Program Files\Autodesk\ApplicationPlugins\RevitParameterInspector.bundle`.
A bundle under `C:\ProgramData\Autodesk\ApplicationPlugins\` is not discovered by Revit 2027.
Keep the existing ProgramData installation for Revit 2024-2026.

Revit 2027 uses `net10.0-windows` with `RevitVersion=2027`. Ribbon, Inspector, Summary,
parameter reading, AI Context, Markdown/XLSX exports and Chinese names passed; see
[verification status](../docs/revit-version-support.md#current-verification-status) for the
full scope and the two retained MSB3277 warnings. Use only one active registration method
per version.
