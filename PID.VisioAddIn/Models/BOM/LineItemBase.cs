#nullable enable
using System.Collections.Generic;

namespace AE.PID.Models.BOM;

/// <summary>
/// The base property for item of BOM.
/// </summary>
public class LineItemBase
{
    /// <summary>
    /// Shape id of the item, used to get extra info from visio.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// A process zone is a group of functional group area in painting such as PT, ED
    /// </summary>
    public string ProcessZone { get; set; } = string.Empty;

    /// <summary>
    /// A functional group is a combination of equipments that targets for the same propose, such as a pre-treatment group.
    /// </summary>
    public string FunctionalGroup { get; set; } = string.Empty;

    /// <summary>
    /// A functional element is an indicator used in electric system for a part item.
    /// </summary>
    public string? FunctionalElement { get; set; } = string.Empty;

    /// <summary>
    /// The material number used in the system to get extra information about the part.
    /// </summary>
    public string MaterialNo { get; set; } = string.Empty;

    /// <summary>
    /// The user friendly name of the part item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The number of the same part used in the source.
    /// </summary>
    public double Count { get; set; }

    /// <summary>
    /// If this line item is a functional element, this property represents the equipment it is related.
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// The linked function elements.
    /// </summary>
    public List<LineItemBase>? Children { get; set; }

    /// <summary>
    /// The type used to specify if the item is treated as a unit equipment which might have several different equipments inside,
    /// or a single equipment that might have some equipments attached to it or not, or a equipment that attached to other single equipment.
    /// </summary>
    public LineItemType Type { get; set; }
}