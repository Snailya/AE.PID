using System;
using System.Collections.Generic;

namespace AE.PID.Models.BOM;

/// <summary>
///     An element is an item in BOM table which represents a equipment, a unit or a logical group.
/// </summary>
public class Element : IComparable<Element>

{
    /// <summary>
    ///     Shape id of the item, used to get extra info from visio.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     A process zone is a group of functional group area in painting such as PT, ED
    /// </summary>
    public string ProcessZone { get; set; } = string.Empty;

    /// <summary>
    ///     A functional group is a combination of equipments that targets for the same propose, such as a pre-treatment group.
    /// </summary>
    public string FunctionalGroup { get; set; } = string.Empty;

    /// <summary>
    ///     A functional element is an indicator used in electric system for a part item.
    /// </summary>
    public string FunctionalElement { get; set; } = string.Empty;

    /// <summary>
    ///     The material number used in the system to get extra information about the part.
    /// </summary>
    public string MaterialNo { get; set; } = string.Empty;

    /// <summary>
    ///     The user friendly name of the part item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The number of the same part used in the source.
    /// </summary>
    public double Count { get; set; }

    /// <summary>
    ///     If this line item is a functional element, this property represents the equipment it is related.
    /// </summary>
    public int ParentId { get; set; }

    /// <summary>
    ///     The type used to specify if the item is treated as a unit equipment which might have several different equipments
    ///     inside,
    ///     or a single equipment that might have some equipments attached to it or not, or a equipment that attached to other
    ///     single equipment.
    /// </summary>
    public ElementType Type { get; set; }

    public int CompareTo(Element other)
    {
        var processZoneComparison = string.Compare(ProcessZone, other.ProcessZone, StringComparison.Ordinal);
        if (processZoneComparison != 0) return processZoneComparison;

        var functionalGroupComparison =
            string.Compare(FunctionalGroup, other.FunctionalGroup, StringComparison.Ordinal);
        if (functionalGroupComparison != 0) return functionalGroupComparison;

        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;

        var functionalElement = string.Compare(FunctionalElement, other.FunctionalElement, StringComparison.Ordinal);
        return functionalElement;
    }
}

public class ElementComparer : IComparer<Element>
{
    public int Compare(Element x, Element y)
    {
        return x.CompareTo(y);
    }
}