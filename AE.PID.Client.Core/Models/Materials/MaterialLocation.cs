﻿namespace AE.PID.Client.Core;

public record MaterialLocation(
    ICompoundKey Id,
    string Code,
    double Quantity,
    double ComputedQuantity,
    string Category,
    string KeyParameters) : MaterialLocationBase(Id, Code, Quantity, ComputedQuantity, Category)
{
    /// <summary>
    ///     The technical data that provides hints when processing material selection.
    /// </summary>
    public string KeyParameters { get; } = KeyParameters;


    /// <summary>
    ///     If this material location is in the internal scope.
    /// </summary>
    public bool IsExcluded { get; set; }
}