using System;
using AE.PID.Visio.Core.Models;

namespace AE.PID.Visio.UI.Avalonia.ViewModels;

public class FunctionViewModel(Function source) : IEquatable<FunctionViewModel>
{
    public int Id { get; } = source.Id;
    public string Name { get; } = source.Name;
    public string Code { get; } = source.Code;
    public string EnglishName { get; } = source.EnglishName;
    public string Description { get; } = source.Description;
    public Function Source { get; } = source;

    public bool Equals(FunctionViewModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Name == other.Name && Code == other.Code && EnglishName == other.EnglishName &&
               Description == other.Description;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FunctionViewModel)obj);
    }

    public override int GetHashCode()
    {
        // Use a combination of properties to generate a hash code
        var hash = 17; // Start with a prime number
        hash = hash * 23 + Id.GetHashCode(); // 23 is a prime number for a good spread
        hash = hash * 23 + Name.GetHashCode();
        hash = hash * 23 + Code.GetHashCode();
        hash = hash * 23 + EnglishName.GetHashCode();
        hash = hash * 23 + Description.GetHashCode();
        return hash;
    }
}