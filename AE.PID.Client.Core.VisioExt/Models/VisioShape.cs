namespace AE.PID.Client.Core.VisioExt;

public class VisioShape(VisioShapeId id, ICollection<VisioShapeCategory> categories)
{
    private static readonly ICollection<VisioShapeCategory> FunctionLocationCategories =
    [
        VisioShapeCategory.ProcessZone, VisioShapeCategory.FunctionalGroup, VisioShapeCategory.Unit,
        VisioShapeCategory.Equipment, VisioShapeCategory.Instrument, VisioShapeCategory.FunctionalElement
    ];

    private static readonly ICollection<VisioShapeCategory> MaterialLocationCategories =
    [
        VisioShapeCategory.Equipment, VisioShapeCategory.Instrument, VisioShapeCategory.FunctionalElement
    ];

    /// <summary>
    ///     The compound id of the shape
    /// </summary>
    public VisioShapeId Id { get; } = id;

    /// <summary>
    ///     The location type that this shape belongs to.
    /// </summary>
    public ICollection<VisioShapeCategory> Categories { get; set; } = categories;

    public ICollection<string> ChangedProperties { get; set; } = [];

    public bool IsFunctionLocation => FunctionLocationCategories.Any(x => Categories.Contains(x));
    public bool IsMaterialLocation => MaterialLocationCategories.Any(x => Categories.Contains(x));

    public override string ToString()
    {
        return
            $"{{Id: {Id}, Categories: '{string.Join(",", Categories)}', ChangedProperties: '{string.Join(",", ChangedProperties)}'}}";
    }
}

public enum VisioShapeCategory
{
    ProcessZone,
    FunctionalGroup,
    Unit,
    Equipment,
    Instrument,
    FunctionalElement,
    Proxy,
    None
}