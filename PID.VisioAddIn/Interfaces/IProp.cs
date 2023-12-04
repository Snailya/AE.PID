using Microsoft.Office.Interop.Visio;

namespace AE.PID.Interfaces;

public interface IProp
{
    /// <summary>
    ///     The name of the section that stores the property, used by <see cref="FullName" />.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    ///     The name of the property that displayed in the spreadsheet's row's first column.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The full name used for name searching, for example <see cref="IVShape.CellExists" />
    /// </summary>
    public string FullName { get; }

    /// <summary>
    ///     Get the <see cref="VisSectionIndices" /> for the property.
    /// </summary>
    /// <returns></returns>
    public VisSectionIndices GetSectionIndices();
}