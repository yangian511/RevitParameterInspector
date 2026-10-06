using Autodesk.Revit.DB;

namespace RevitParameterInspector.Revit.Compatibility;

/// <summary>
/// Single seam for Revit-version differences (HANDOFF Section 7). V1's minimum supported
/// version is Revit 2024, where ElementId storage is already 64-bit, so there is no
/// IntegerValue/Value branching to do today, including Revit 2027. GetIdValue and
/// CreateElementId share the same API across 2024-2027. Future version-specific quirks
/// should be added here rather than scattered across readers/builders.
/// </summary>
public static class RevitCompatibility
{
    public static long GetIdValue(ElementId id) => id.Value;

    public static ElementId CreateElementId(long value) => new(value);
}
