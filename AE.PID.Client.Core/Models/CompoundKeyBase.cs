using System.Runtime.Serialization;

namespace AE.PID.Client.Core;

[DataContract]
public abstract class CompoundKeyBase : ICompoundKey
{
    public abstract int ComputedId { get; }

    // 必须重写 Equals 方法
    public int CompareTo(ICompoundKey other)
    {
        return ComputedId.CompareTo(other.ComputedId);
    }

    public override bool Equals(object obj)
    {
        if (obj is CompoundKeyBase other) return ComputedId == other.ComputedId;
        return false;
    }

    // 必须重写 GetHashCode 方法
    public override int GetHashCode()
    {
        return ComputedId;
    }
}