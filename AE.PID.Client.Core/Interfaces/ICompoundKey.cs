using System;

namespace AE.PID.Client.Core;

/// <summary>
///     For some source there is no unique id property but only compound key, therefore a computed id is used that
///     represents this compound key and unique inside the scope
/// </summary>
public interface ICompoundKey : IComparable<ICompoundKey>
{
    public int ComputedId { get; }
}