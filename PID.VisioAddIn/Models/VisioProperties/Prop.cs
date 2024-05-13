using System;
using AE.PID.Interfaces;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Models;

public abstract class Prop(string name, string prefix) : IProp
{
    public string Prefix { get; } = prefix;
    public string Name { get; } = name;
    public string FullName => $"{Prefix}.{Name}";

    public VisSectionIndices GetSectionIndices()
    {
        return Prefix switch
        {
            "Actions" => VisSectionIndices.visSectionAction,
            "User" => VisSectionIndices.visSectionUser,
            "Prop" => VisSectionIndices.visSectionProp,
            "" => VisSectionIndices.visSectionObject,
            _ => throw new ArgumentOutOfRangeException(nameof(Prefix))
        };
    }
}