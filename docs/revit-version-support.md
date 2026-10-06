# Revit Version Support

## Supported versions

| Revit Version | Target Framework | How to select it |
|---|---|---|
| 2024 | `net48` | default for this TFM |
| 2025 | `net8.0-windows` | default for this TFM |
| 2026 | `net8.0-windows` | `-p:RevitVersion=2026` |
| 2027 | `net10.0-windows` | `-p:RevitVersion=2027` (.NET 10 SDK required) |

See [build-guide.md](build-guide.md) for the exact build commands and how `RevitInstallDir`
resolution works.

## Why the split

- **Revit 2024** introduced 64-bit `ElementId` support; `ElementId.IntegerValue` became
  obsolete in favor of `ElementId.Value` (a `long`). This project's minimum supported version
  is 2024, so `Core` models already store ids as `long` everywhere and there is no
  `IntegerValue`/`Value` branching to do.
- **Revit 2025** moved the Revit API itself onto .NET 8, so 2025/2026 add-ins must be built as
  `net8.0-windows`, not `net48`. 2024 remains on `net48`/.NET Framework.
- **Revit 2026** also targets `net8.0-windows`; the `RevitVersion` MSBuild property
  distinguishes it from 2025 for the handful of cases where API behavior differs between the
  two.
- **Revit 2027** moves to .NET 10. The installed `27.2.0.0` API references
  `System.Runtime 10.0.0.0`; compiling it as `net8.0-windows` fails with `CS1705`.
  The Revit project therefore targets `net10.0-windows` for `RevitVersion=2027`, while
  reusing the API-free shared projects' .NET 8 outputs.
  See [Autodesk's .NET 10 migration guidance](https://help.autodesk.com/cloudhelp/2027/ENU/Revit-WhatsNew/files/GUID-8D7A4715-EAF8-4BD1-BE78-061F900D0BCE.htm).

```powershell
dotnet build src/RevitParameterInspector.Revit -f net10.0-windows -p:RevitVersion=2027 -c Release
```

The default API directory is `C:\Program Files\Autodesk\Revit 2027`; both `RevitAPI.dll`
and `RevitAPIUI.dll` resolve there. Use `-p:RevitInstallDir=...` to override it.

## Where version differences live

All Revit-version-specific code should go in
`src/RevitParameterInspector.Revit/Compatibility/RevitCompatibility.cs`, gated by the
`REVIT2024` / `REVIT2025` / `REVIT2026` / `REVIT2027` and cumulative
`REVIT2024_OR_GREATER` through `REVIT2027_OR_GREATER`
define constants the `.csproj` sets based on the resolved version (see build-guide.md's table).
Builders and readers elsewhere should call into `RevitCompatibility` rather than `#if` on Revit
version themselves. `GetIdValue(ElementId)` and `CreateElementId(long)` use the same 64-bit
API in 2024-2027. The complete add-in compiles against the 2027 API without an additional
version-specific API branch. This proves source compatibility for the APIs used here, not
that all Revit 2026 APIs or runtime behaviors are unchanged in 2027.

## Current verification status

Revit 2027 Release compilation against installed API `27.2.0.0`, using SDK `10.0.401`,
succeeds with `net10.0-windows` and `RevitVersion=2027`.

On 2026-10-06, the maintainer reported a successful controlled smoke test inside actual
Autodesk Revit 2027. Both per-user `.addin` loading and machine-wide ApplicationPlugins
bundle loading passed. The verified machine-wide bundle location is:

```text
C:\Program Files\Autodesk\ApplicationPlugins\RevitParameterInspector.bundle
```

Revit 2027 does not discover machine-wide bundles under `ProgramData`. The existing
`R2027` package registration and relative manifest paths required no changes.

The runtime test passed ribbon registration, Inspector, Summary, wall inspection, parameter
reading, instance/type recognition, AI Context generation, Markdown export, Excel XLSX
export and Chinese parameter display. No runtime assembly exception occurred during these
tests. This is evidence for the exercised workflows, not exhaustive testing of every command.

Two `MSB3277` build warnings remain and are not suppressed:

- `Microsoft.VisualBasic`: the .NET 10 framework facade and RevitAPI reference `10.0.0.0`,
  while RevitAPIUI and other Autodesk dependencies reference `10.1.0.0`.
- `System.Drawing`: the framework facade is `4.0.0.0`, while Autodesk dependencies
  reference `10.0.0.0`.

The tested runtime workflows succeeded despite these warnings. Keep the warnings visible;
no binding workaround or replacement framework assembly is included.

The 2024/2025/2026 framework, effective version, API paths and compilation symbols are
checked through MSBuild evaluation. Their API DLLs are not installed in the build-verification
environment, so their Revit-dependent builds are **not verified**. No newer API DLLs were
substituted for them. The API-free projects compile for both `net48` and `net8.0-windows`.
No runtime smoke test is recorded here for 2024-2026. View/sheet scans, selection/reselection
and JSON export are outside the reported 2027 smoke-test scope.

Per-user `%APPDATA%` registration remains available. See
[Autodesk's SDK changes](https://blog.autodesk.io/revit-2027-sdk-net-10-api-changes-and-additions/)
and [install/README.md](../install/README.md).
