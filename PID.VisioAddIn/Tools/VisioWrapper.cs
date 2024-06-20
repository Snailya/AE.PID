using Microsoft.Office.Interop.Visio;

namespace AE.PID.Tools;

internal static class VisioWrapper
{
    public static bool CellExistsN(this IVShape shape, string propName, VisExistsFlags flags)
    {
        return shape.CellExists[propName, (short)flags] ==
               (short)VBABool.True;
    }

    public static Cell CellsSRCN(this IVShape shape, VisSectionIndices sectionIndices, VisRowIndices rowIndices,
        VisCellIndices cellIndices)
    {
        return shape.CellsSRC[(short)sectionIndices, (short)rowIndices, (short)cellIndices];
    }

    private enum VBABool : short
    {
        True = -1,
        False = 0
    }
}