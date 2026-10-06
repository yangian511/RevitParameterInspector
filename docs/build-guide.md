# Build Guide

For a quick "clone and try it" walkthrough see [getting-started.md](getting-started.md). This
doc goes deeper into how the solution is put together and how to build it deliberately for a
specific Revit version.

## Solution layout

```text
RevitParameterInspector.sln
└─ src/
   ├─ RevitParameterInspector.Core        (no Revit API dependency)
   ├─ RevitParameterInspector.Dictionary   → Core
   ├─ RevitParameterInspector.Export       → Core
   ├─ RevitParameterInspector.UI           → Core, Export
   └─ RevitParameterInspector.Revit        → Core, UI, Export, Dictionary, RevitAPI, RevitAPIUI
```

Only `RevitParameterInspector.Revit` references `RevitAPI.dll`/`RevitAPIUI.dll`. Every other
project is plain .NET and builds without Revit installed at all - useful for iterating on
Core/Export/Dictionary/UI logic outside Revit.

`Directory.Build.props` at the repo root applies shared settings to every project:
`LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `Deterministic=true`, and the
shared `Version`/`RevitParameterInspectorVersion` (currently `0.4.5`).

## Multi-targeting and Revit version resolution

The shared projects target `net48` and `net8.0-windows`. The Revit project keeps those
defaults, and enables `net10.0-windows` when `RevitVersion=2027` is supplied. Its .NET 10
build references the API-free shared projects' compatible .NET 8 outputs; no UI or export
behavior changes are needed. Older-version builds do not require the .NET 10 SDK.
`RevitParameterInspector.Revit.csproj` maps frameworks to Revit versions like this:

| `TargetFramework` | `RevitVersion` property | Resolves to | Define constants |
|---|---|---|---|
| `net48` | (ignored) | 2024 | `REVIT2024`, `REVIT2024_OR_GREATER` |
| `net8.0-windows` | unset | 2025 | `REVIT2025`, `REVIT2024_OR_GREATER`, `REVIT2025_OR_GREATER` |
| `net8.0-windows` | `2026` | 2026 | `REVIT2026`, `REVIT2024_OR_GREATER`, `REVIT2025_OR_GREATER`, `REVIT2026_OR_GREATER` |
| `net10.0-windows` | `2027` | 2027 | `REVIT2027`, `REVIT2024_OR_GREATER`, `REVIT2025_OR_GREATER`, `REVIT2026_OR_GREATER`, `REVIT2027_OR_GREATER` |

The resolved version also drives `RevitInstallDir` (default
`C:\Program Files\Autodesk\Revit <version>`), which is where `RevitAPI.dll`/`RevitAPIUI.dll`
are picked up (`Private=false`, `SpecificVersion=false`, so they aren't copied to the output -
Revit provides them at runtime). Version-specific code should branch on the define constants
inside `RevitParameterInspector.Revit/Compatibility`, not be scattered across builders/readers.

Revit 2027 requires the .NET 10 SDK and APIs from `C:\Program Files\Autodesk\Revit 2027`
by default. [Autodesk's migration guidance](https://help.autodesk.com/cloudhelp/2027/ENU/Revit-WhatsNew/files/GUID-8D7A4715-EAF8-4BD1-BE78-061F900D0BCE.htm)
explains the runtime change. `net8.0-windows` with 2027 APIs is unsupported and produces
assembly-version errors; do not suppress those errors or substitute older API DLLs.

## Building one target at a time

Always pass `-f` and, for 2026/2027, `-p:RevitVersion=...` explicitly - building without `-f` attempts every
`TargetFramework` in the list, which requires `RevitInstallDir` to resolve for more than one
Revit version at once:

```bash
dotnet build src/RevitParameterInspector.Revit -f net48                                   # 2024
dotnet build src/RevitParameterInspector.Revit -f net8.0-windows                          # 2025
dotnet build src/RevitParameterInspector.Revit -f net8.0-windows -p:RevitVersion=2026     # 2026
dotnet build src/RevitParameterInspector.Revit -f net10.0-windows -p:RevitVersion=2027 -c Release # 2027
dotnet build src/RevitParameterInspector.Revit -f net8.0-windows -p:RevitInstallDir="D:\Autodesk\Revit 2025"
```

`-c Release` builds a release configuration; omit it for `Debug` (the default). The other four
projects (`Core`, `Dictionary`, `Export`, `UI`) don't need Revit installed and can be built the
same way, or built as part of building `RevitParameterInspector.Revit` since it references all
of them.

To build everything Visual Studio can (this still needs `RevitAPI.dll` resolvable for whichever
Revit-dependent target you pick, or it will fail on that one target only), open
`RevitParameterInspector.sln` directly.

## Output layout

Building `RevitParameterInspector.Revit` for a given `TargetFramework` produces, under
`src/RevitParameterInspector.Revit/bin/<Debug|Release>/<TargetFramework>/`:

- `RevitParameterInspector.Revit.dll` (the add-in assembly, referenced by the `.addin`
  manifest - see [getting-started.md](getting-started.md))
- `RevitParameterInspector.Core.dll`, `.UI.dll`, `.Export.dll`, `.Dictionary.dll`
- `dictionary/` - a copy of the repo-root `dictionary/` folder, so `DictionaryEngine` can find
  it next to the running assembly at runtime (see `RevitParameterInspector.Revit.csproj`'s
  `CopyToOutputDirectory` item)
- `ClosedXML.dll` and its own dependencies (used by `RevitParameterInspector.Export` for Excel
  export)

## Tests

There is no dedicated test project in the solution yet. Verification so far has been manual:
building each project independently, and exercising `Core`/`Export`/`UI` logic (which have no
Revit API dependency) via throwaway console harnesses that construct an `ElementContextSnapshot`
by hand and feed it through the real exporters/`ObjectInspector`/`MainWindowViewModel`. The
`RevitParameterInspector.Revit` project itself (builders, readers, the external command) has
not been exercised inside an actual running Revit instance.

Revit 2027 compilation has been checked against installed API version `27.2.0.0` with SDK
`10.0.401`. Packaging with `-Versions 2027 -Configuration Release` is also checked.
This does not verify loading or behavior inside Revit. See
[revit-version-support.md](revit-version-support.md#current-verification-status) for limits
and remaining reference warnings.
