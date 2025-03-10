using System;
using AE.PID.Client.Core;

namespace AE.PID.Client.UI.Avalonia;

public class FunctionViewModel(int? id, string name, string code, string englishName, string description)
    : IEquatable<FunctionViewModel>
{
    public FunctionViewModel(Function source) : this(source.Id, source.Name, source.Code, source.EnglishName,
        source.Description)
    {
        Source = source;
    }

    public int? Id { get; } = id;
    public string Name { get; } = name;
    public string Code { get; } = code;
    public string EnglishName { get; } = englishName;
    public string Description { get; } = description;
    public Function? Source { get; }

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