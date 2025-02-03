namespace AE.PID.Client.Core;

public abstract record LocationBase(ICompoundKey Id)
{
    public ICompoundKey Id { get; } = Id;
}